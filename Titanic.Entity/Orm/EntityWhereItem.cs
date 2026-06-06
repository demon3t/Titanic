using Titanic.Db;
using Titanic.Db.Abstractions;
using Titanic.Db.Enums;

namespace Titanic.Entity.Orm
{
    /// <summary>
    /// ORM wrapper over a WHERE/AND/OR condition.
    /// </summary>
    public class EntityWhereItem
    {
        private readonly EntitySelectBuilder _builder;
        private readonly QueryExpression _leftExpression;
        private readonly EntityWhereConnector _connector;
        private bool _negated;

        internal EntityWhereItem(
            EntitySelectBuilder builder,
            QueryExpression leftExpression,
            EntityWhereConnector connector)
        {
            _builder = builder;
            _leftExpression = leftExpression;
            _connector = connector;
        }

        public EntityWhereItem Not()
        {
            _negated = true;
            return this;
        }

        public EntitySelectBuilder IsEqual(object? value)
        {
            return Add(value == null
                ? BuildUnary(ConditionOperator.IsNull)
                : BuildBinary(ConditionOperator.Equal, Column.Parameter(value)));
        }

        public EntitySelectBuilder IsEqual(string targetAlias, string targetColumnName)
        {
            return Add(BuildBinary(ConditionOperator.Equal, Column.Name(targetAlias, targetColumnName)));
        }

        public EntitySelectBuilder IsEqual(QueryExpression value)
        {
            return Add(BuildBinary(ConditionOperator.Equal, value));
        }

        public EntitySelectBuilder IsGreaterThan(object? value)
        {
            return Add(BuildBinary(ConditionOperator.GreaterThan, Column.Parameter(value)));
        }

        public EntitySelectBuilder IsGreaterOrEqual(object? value)
        {
            return Add(BuildBinary(ConditionOperator.GreaterThanOrEqual, Column.Parameter(value)));
        }

        public EntitySelectBuilder IsLess(object? value)
        {
            return Add(BuildBinary(ConditionOperator.LessThan, Column.Parameter(value)));
        }

        public EntitySelectBuilder IsLessOrEqual(object? value)
        {
            return Add(BuildBinary(ConditionOperator.LessThanOrEqual, Column.Parameter(value)));
        }

        public EntitySelectBuilder IsContains(object? value)
        {
            var expression = BuildBinary(ConditionOperator.Contains, Column.Parameter(value));
            if (!_negated)
            {
                return Add(expression);
            }

            _negated = false;
            return Add(QueryExpression.Not(expression));
        }

        public EntitySelectBuilder IsNull()
        {
            var op = _negated ? ConditionOperator.IsNotNull : ConditionOperator.IsNull;
            _negated = false;
            return Add(BuildUnary(op));
        }

        public EntitySelectBuilder IsNotNull()
        {
            var op = _negated ? ConditionOperator.IsNull : ConditionOperator.IsNotNull;
            _negated = false;
            return Add(BuildUnary(op));
        }

        public EntitySelectBuilder In(BaseQuery subQuery)
        {
            var op = _negated ? ConditionOperator.NotIn : ConditionOperator.In;
            _negated = false;
            return Add(BuildBinary(op, QueryExpression.SubQuery(subQuery)));
        }

        public EntitySelectBuilder Between(object? low, object? high)
        {
            var expression = QueryExpression.And(
                BuildBinary(ConditionOperator.GreaterThanOrEqual, Column.Parameter(low)),
                BuildBinary(ConditionOperator.LessThanOrEqual, Column.Parameter(high)));

            if (!_negated)
            {
                return Add(expression);
            }

            _negated = false;
            return Add(QueryExpression.Not(expression));
        }

        private EntitySelectBuilder Add(QueryExpression expression)
        {
            if (_negated)
            {
                expression = QueryExpression.Not(expression);
                _negated = false;
            }

            return _builder.AddWhereExpression(expression, _connector);
        }

        private QueryExpression BuildBinary(ConditionOperator op, QueryExpression value)
        {
            return QueryExpression.Binary(_leftExpression, op, value);
        }

        private QueryExpression BuildUnary(ConditionOperator op)
        {
            return EntityQueryExpression.Unary(op, _leftExpression);
        }
    }
}
