using Titanic.Db.Abstractions;
using Titanic.Db.Enums;

namespace Titanic.Entity.Orm
{
    internal sealed class EntityQueryExpression : QueryExpression
    {
        private EntityQueryExpression(
            ExpressionType expressionType,
            ConditionOperator? conditionOperator = null,
            string? op = null,
            IEnumerable<QueryExpression>? children = null)
            : base(string.Empty, expressionType)
        {
            Sql = null;
            ConditionOperatorType = conditionOperator;
            Operator = op;

            if (children != null)
            {
                Expressions.AddRange(children);
            }
        }

        public static QueryExpression Unary(ConditionOperator op, QueryExpression expression)
        {
            return new EntityQueryExpression(
                ExpressionType.Unary,
                conditionOperator: op,
                children: new[] { expression });
        }
    }
}
