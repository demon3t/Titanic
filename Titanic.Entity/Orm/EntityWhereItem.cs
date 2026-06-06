using Titanic.Db;
using Titanic.Db.Abstractions;

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
            return Add(EntityComparisonExpressionBuilder.Build(_leftExpression, EntityComparisonType.Equal, value));
        }

        /// <summary>
        /// Добавить условие равенства с другой колонкой.
        /// </summary>
        public EntitySelectBuilder IsEqual(string targetAlias, string targetColumnName)
        {
            return Add(EntityComparisonExpressionBuilder.Build(
                _leftExpression,
                EntityComparisonType.Equal,
                Column.Name(targetAlias, targetColumnName)));
        }

        /// <summary>
        /// Добавить условие равенства с выражением.
        /// </summary>
        public EntitySelectBuilder IsEqual(QueryExpression value)
        {
            return Add(EntityComparisonExpressionBuilder.Build(_leftExpression, EntityComparisonType.Equal, value));
        }

        /// <summary>
        /// Добавить условие больше.
        /// </summary>
        public EntitySelectBuilder IsGreaterThan(object? value)
        {
            return Add(EntityComparisonExpressionBuilder.Build(_leftExpression, EntityComparisonType.GreaterThan, value));
        }

        /// <summary>
        /// Добавить условие больше или равно.
        /// </summary>
        public EntitySelectBuilder IsGreaterOrEqual(object? value)
        {
            return Add(EntityComparisonExpressionBuilder.Build(
                _leftExpression,
                EntityComparisonType.GreaterThanOrEqual,
                value));
        }

        /// <summary>
        /// Добавить условие меньше.
        /// </summary>
        public EntitySelectBuilder IsLess(object? value)
        {
            return Add(EntityComparisonExpressionBuilder.Build(_leftExpression, EntityComparisonType.LessThan, value));
        }

        /// <summary>
        /// Добавить условие меньше или равно.
        /// </summary>
        public EntitySelectBuilder IsLessOrEqual(object? value)
        {
            return Add(EntityComparisonExpressionBuilder.Build(
                _leftExpression,
                EntityComparisonType.LessThanOrEqual,
                value));
        }

        /// <summary>
        /// Добавить условие поиска по вхождению без ручного указания шаблона LIKE.
        /// </summary>
        public EntitySelectBuilder IsContains(object? value)
        {
            return Add(EntityComparisonExpressionBuilder.Build(_leftExpression, EntityComparisonType.Contains, value));
        }

        /// <summary>
        /// Добавить условие поиска по началу строки.
        /// </summary>
        public EntitySelectBuilder IsStartsWith(object? value)
        {
            return Add(EntityComparisonExpressionBuilder.Build(_leftExpression, EntityComparisonType.StartsWith, value));
        }

        /// <summary>
        /// Добавить условие поиска по концу строки.
        /// </summary>
        public EntitySelectBuilder IsEndsWith(object? value)
        {
            return Add(EntityComparisonExpressionBuilder.Build(_leftExpression, EntityComparisonType.EndsWith, value));
        }

        /// <summary>
        /// Добавить условие IS NULL.
        /// </summary>
        public EntitySelectBuilder IsNull()
        {
            var comparisonType = _negated
                ? EntityComparisonType.IsNotNull
                : EntityComparisonType.IsNull;
            _negated = false;
            return Add(EntityComparisonExpressionBuilder.BuildNull(_leftExpression, comparisonType));
        }

        /// <summary>
        /// Добавить условие IS NOT NULL.
        /// </summary>
        public EntitySelectBuilder IsNotNull()
        {
            var comparisonType = _negated
                ? EntityComparisonType.IsNull
                : EntityComparisonType.IsNotNull;
            _negated = false;
            return Add(EntityComparisonExpressionBuilder.BuildNull(_leftExpression, comparisonType));
        }

        /// <summary>
        /// Добавить условие IN по подзапросу.
        /// </summary>
        public EntitySelectBuilder In(BaseQuery subQuery)
        {
            var comparisonType = _negated
                ? EntityComparisonType.NotIn
                : EntityComparisonType.In;
            _negated = false;
            return Add(EntityComparisonExpressionBuilder.Build(_leftExpression, comparisonType, subQuery));
        }

        /// <summary>
        /// Добавить условие BETWEEN.
        /// </summary>
        public EntitySelectBuilder Between(object? low, object? high)
        {
            var expression = EntityComparisonExpressionBuilder.BuildBetween(_leftExpression, low, high);

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
    }
}
