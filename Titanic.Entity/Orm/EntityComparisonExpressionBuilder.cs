using Titanic.Db;
using Titanic.Db.Abstractions;
using Titanic.Db.Enums;

namespace Titanic.Entity.Orm
{
    /// <summary>
    /// Внутренний builder выражений Entity ORM по типу сравнения фильтра.
    /// </summary>
    internal static class EntityComparisonExpressionBuilder
    {
        /// <summary>
        /// Построить выражение сравнения по значению фильтра.
        /// </summary>
        /// <param name="left"> Левая часть выражения. </param>
        /// <param name="comparisonType"> Тип сравнения Entity ORM. </param>
        /// <param name="value"> Значение фильтра. </param>
        /// <returns> Построенное выражение. </returns>
        public static QueryExpression Build(QueryExpression left, EntityComparisonType comparisonType, object? value)
        {
            return comparisonType switch
            {
                EntityComparisonType.Equal when value == null => BuildNull(left, EntityComparisonType.IsNull),
                EntityComparisonType.NotEqual when value == null => BuildNull(left, EntityComparisonType.IsNotNull),
                EntityComparisonType.Contains => BuildBinary(left, comparisonType, Column.Parameter(EntityStringPatternBuilder.Contains(value))),
                EntityComparisonType.StartsWith => BuildBinary(left, comparisonType, Column.Parameter(EntityStringPatternBuilder.StartsWith(value))),
                EntityComparisonType.EndsWith => BuildBinary(left, comparisonType, Column.Parameter(EntityStringPatternBuilder.EndsWith(value))),
                EntityComparisonType.IsNull or EntityComparisonType.IsNotNull => BuildNull(left, comparisonType),
                EntityComparisonType.In or EntityComparisonType.NotIn => BuildIn(left, comparisonType, value),
                _ => BuildBinary(left, comparisonType, Column.Parameter(value))
            };
        }

        /// <summary>
        /// Построить выражение сравнения по готовому выражению правой части.
        /// </summary>
        /// <param name="left"> Левая часть выражения. </param>
        /// <param name="comparisonType"> Тип сравнения Entity ORM. </param>
        /// <param name="right"> Правая часть выражения. </param>
        /// <returns> Построенное выражение. </returns>
        public static QueryExpression Build(QueryExpression left, EntityComparisonType comparisonType, QueryExpression right)
        {
            return BuildBinary(left, comparisonType, right);
        }

        /// <summary>
        /// Построить BETWEEN-выражение.
        /// </summary>
        /// <param name="left"> Левая часть выражения. </param>
        /// <param name="from"> Нижняя граница. </param>
        /// <param name="to"> Верхняя граница. </param>
        /// <returns> Построенное выражение. </returns>
        public static QueryExpression BuildBetween(QueryExpression left, object? from, object? to)
        {
            return QueryExpression.And(
                Build(left, EntityComparisonType.GreaterThanOrEqual, from),
                Build(left, EntityComparisonType.LessThanOrEqual, to));
        }

        /// <summary>
        /// Построить NULL-проверку.
        /// </summary>
        /// <param name="left"> Левая часть выражения. </param>
        /// <param name="comparisonType"> Тип NULL-проверки. </param>
        /// <returns> Построенное выражение. </returns>
        public static QueryExpression BuildNull(QueryExpression left, EntityComparisonType comparisonType)
        {
            return EntityQueryExpression.Unary(MapUnaryOperator(comparisonType), left);
        }

        private static QueryExpression BuildBinary(
            QueryExpression left,
            EntityComparisonType comparisonType,
            QueryExpression right)
        {
            return QueryExpression.Binary(left, MapBinaryOperator(comparisonType), right);
        }

        private static QueryExpression BuildIn(QueryExpression left, EntityComparisonType comparisonType, object? value)
        {
            if (value is not BaseQuery subQuery)
            {
                throw new NotSupportedException(
                    "Entity ORM currently supports IN and NOT IN only with BaseQuery values.");
            }

            return BuildBinary(left, comparisonType, QueryExpression.SubQuery(subQuery));
        }

        private static ConditionOperator MapBinaryOperator(EntityComparisonType comparisonType)
        {
            return comparisonType switch
            {
                EntityComparisonType.Equal => ConditionOperator.Equal,
                EntityComparisonType.NotEqual => ConditionOperator.NotEqual,
                EntityComparisonType.GreaterThan => ConditionOperator.GreaterThan,
                EntityComparisonType.GreaterThanOrEqual => ConditionOperator.GreaterThanOrEqual,
                EntityComparisonType.LessThan => ConditionOperator.LessThan,
                EntityComparisonType.LessThanOrEqual => ConditionOperator.LessThanOrEqual,
                EntityComparisonType.In => ConditionOperator.In,
                EntityComparisonType.NotIn => ConditionOperator.NotIn,
                EntityComparisonType.Like => ConditionOperator.Like,
                EntityComparisonType.NotLike => ConditionOperator.NotLike,
                EntityComparisonType.ILike => ConditionOperator.ILike,
                EntityComparisonType.Contains => ConditionOperator.ILike,
                EntityComparisonType.StartsWith => ConditionOperator.ILike,
                EntityComparisonType.EndsWith => ConditionOperator.ILike,
                _ => throw new NotSupportedException(
                    $"Entity comparison '{comparisonType}' cannot be mapped to a binary SQL operator.")
            };
        }

        private static ConditionOperator MapUnaryOperator(EntityComparisonType comparisonType)
        {
            return comparisonType switch
            {
                EntityComparisonType.IsNull => ConditionOperator.IsNull,
                EntityComparisonType.IsNotNull => ConditionOperator.IsNotNull,
                _ => throw new NotSupportedException(
                    $"Entity comparison '{comparisonType}' cannot be mapped to a unary SQL operator.")
            };
        }
    }
}
