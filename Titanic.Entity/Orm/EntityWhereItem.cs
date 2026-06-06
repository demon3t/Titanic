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

        /// <summary>
        /// Инвертировать следующее условие.
        /// </summary>
        public EntityWhereItem Not()
        {
            _negated = true;
            return this;
        }

        /// <summary>
        /// Добавить условие равенства.
        /// </summary>
        public EntitySelectBuilder IsEqual(object? value)
        {
            return Add(value == null
                ? BuildUnary(ConditionOperator.IsNull)
                : BuildBinary(ConditionOperator.Equal, Column.Parameter(value)));
        }

        /// <summary>
        /// Добавить условие равенства с другой колонкой.
        /// </summary>
        public EntitySelectBuilder IsEqual(string targetAlias, string targetColumnName)
        {
            return Add(BuildBinary(ConditionOperator.Equal, Column.Name(targetAlias, targetColumnName)));
        }

        /// <summary>
        /// Добавить условие равенства с выражением.
        /// </summary>
        public EntitySelectBuilder IsEqual(QueryExpression value)
        {
            return Add(BuildBinary(ConditionOperator.Equal, value));
        }

        /// <summary>
        /// Добавить условие больше.
        /// </summary>
        public EntitySelectBuilder IsGreaterThan(object? value)
        {
            return Add(BuildBinary(ConditionOperator.GreaterThan, Column.Parameter(value)));
        }

        /// <summary>
        /// Добавить условие больше или равно.
        /// </summary>
        public EntitySelectBuilder IsGreaterOrEqual(object? value)
        {
            return Add(BuildBinary(ConditionOperator.GreaterThanOrEqual, Column.Parameter(value)));
        }

        /// <summary>
        /// Добавить условие меньше.
        /// </summary>
        public EntitySelectBuilder IsLess(object? value)
        {
            return Add(BuildBinary(ConditionOperator.LessThan, Column.Parameter(value)));
        }

        /// <summary>
        /// Добавить условие меньше или равно.
        /// </summary>
        public EntitySelectBuilder IsLessOrEqual(object? value)
        {
            return Add(BuildBinary(ConditionOperator.LessThanOrEqual, Column.Parameter(value)));
        }

        /// <summary>
        /// Добавить условие поиска по вхождению без ручного указания шаблона LIKE.
        /// </summary>
        public EntitySelectBuilder IsContains(object? value)
        {
            return Add(BuildBinary(ConditionOperator.Contains, Column.Parameter(value)));
        }

        /// <summary>
        /// Добавить условие поиска по началу строки.
        /// </summary>
        public EntitySelectBuilder IsStartsWith(object? value)
        {
            return Add(BuildBinary(ConditionOperator.StartsWith, Column.Parameter(value)));
        }

        /// <summary>
        /// Добавить условие поиска по концу строки.
        /// </summary>
        public EntitySelectBuilder IsEndsWith(object? value)
        {
            return Add(BuildBinary(ConditionOperator.EndsWith, Column.Parameter(value)));
        }

        /// <summary>
        /// Добавить условие IS NULL.
        /// </summary>
        public EntitySelectBuilder IsNull()
        {
            var op = _negated ? ConditionOperator.IsNotNull : ConditionOperator.IsNull;
            _negated = false;
            return Add(BuildUnary(op));
        }

        /// <summary>
        /// Добавить условие IS NOT NULL.
        /// </summary>
        public EntitySelectBuilder IsNotNull()
        {
            var op = _negated ? ConditionOperator.IsNull : ConditionOperator.IsNotNull;
            _negated = false;
            return Add(BuildUnary(op));
        }

        /// <summary>
        /// Добавить условие IN по подзапросу.
        /// </summary>
        public EntitySelectBuilder In(BaseQuery subQuery)
        {
            var op = _negated ? ConditionOperator.NotIn : ConditionOperator.In;
            _negated = false;
            return Add(BuildBinary(op, QueryExpression.SubQuery(subQuery)));
        }

        /// <summary>
        /// Добавить условие BETWEEN.
        /// </summary>
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
