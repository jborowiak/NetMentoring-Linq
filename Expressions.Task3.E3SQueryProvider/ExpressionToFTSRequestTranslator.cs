using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Expressions.Task3.E3SQueryProvider
{
    public class ExpressionToFtsRequestTranslator : ExpressionVisitor
    {
        readonly StringBuilder _resultStringBuilder;

        public ExpressionToFtsRequestTranslator()
        {
            _resultStringBuilder = new StringBuilder();
        }

        public string Translate(Expression exp)
        {
            Visit(exp);

            return _resultStringBuilder.ToString();
        }

        #region protected methods

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(Queryable)
                && node.Method.Name == "Where")
            {
                var predicate = node.Arguments[1];
                Visit(predicate);

                return node;
            }

            switch (node.Method.Name)
            {
                case "Equals":
                {
                    return BuildInclusionOperation(node, "(", ")");
                }
                case "StartsWith":
                {
                    return BuildInclusionOperation(node, "(", "*)");
                    }
                case "EndsWith":
                {
                    return BuildInclusionOperation(node, "(*", ")");
                    }
                case "Contains":
                {
                    return BuildInclusionOperation(node, "(*", "*)");
                    }
                default:
                    return base.VisitMethodCall(node);
            }
        }

        private Expression BuildInclusionOperation(MethodCallExpression node, string leftAppender, string rightAppender)
        {
            var value = node.Arguments[0];

            Visit(node.Object);
            _resultStringBuilder.Append(leftAppender);
            Visit(value);
            _resultStringBuilder.Append(rightAppender);

            return node;
        }


        protected override Expression VisitBinary(BinaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Equal:
                    var nodeTypes = new[] {node.Left.NodeType, node.Right.NodeType};
                    if (!nodeTypes.Contains(ExpressionType.MemberAccess) ||
                        !nodeTypes.Contains(ExpressionType.Constant))
                    {
                        throw new NotSupportedException(
                            $"One operand should be property or field and the other should be field: {node.NodeType}");
                    }
                    if (nodeTypes.First() == ExpressionType.MemberAccess)
                    {
                        Visit(node.Left);
                        _resultStringBuilder.Append("(");
                        Visit(node.Right);
                        _resultStringBuilder.Append(")");
                    }
                    else
                    {
                        Visit(node.Right);
                        _resultStringBuilder.Append("(");
                        Visit(node.Left);
                        _resultStringBuilder.Append(")");
                    }
                    break;

                default:
                    throw new NotSupportedException($"Operation '{node.NodeType}' is not supported");
            };

            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            _resultStringBuilder.Append(node.Member.Name).Append(":");

            return base.VisitMember(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            _resultStringBuilder.Append(node.Value);

            return node;
        }

        #endregion
    }
}
