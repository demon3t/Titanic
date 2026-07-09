using Titanic.Db.Abstractions;
using Titanic.Db.Enums;

namespace Titanic.Db.Builders
{
    /// <summary>
    /// Билдер одного условия WHERE.
    /// Для отрицания следующего условия вызывайте <see cref="Not"/>.
    /// </summary>
    /// <typeparam name="TQuery">Тип запроса.</typeparam>
    public class WhereItem<TQuery> where TQuery : BaseQuery
    {
        private readonly string _alias;
        private readonly string _columnName;
        private readonly Func<QueryExpression, TQuery> _addWhere;
        private bool _negated;

        internal WhereItem(TQuery query, string alias, string columnName, Func<QueryExpression, TQuery> addWhere)
        {
            _alias = alias;
            _columnName = columnName;
            _addWhere = addWhere;
        }

        /// <summary>
        /// Включить отрицание для следующего условия.
        /// </summary>
        public WhereItem<TQuery> Not()
        {
            _negated = true;
            return this;
        }

        /// <summary>Добавить условие равенства.</summary>
        public TQuery IsEqual(object? value)
            => AddExpression(ComparisonExpr(value, ConditionOperator.Equal));

        /// <summary>Добавить условие равенства с другой колонкой.</summary>
        public TQuery IsEqual(string targetAlias, string targetColumnName)
            => AddExpression(ComparisonExpr(targetAlias, targetColumnName, ConditionOperator.Equal));

        /// <summary>Добавить условие равенства с выражением.</summary>
        public TQuery IsEqual(QueryExpression value)
            => AddExpression(ComparisonExpr(value, ConditionOperator.Equal));

        /// <summary>Добавить условие больше.</summary>
        public TQuery IsGreaterThan(object? value)
            => AddExpression(ComparisonExpr(value, ConditionOperator.GreaterThan));

        /// <summary>Добавить условие больше с выражением.</summary>
        public TQuery IsGreaterThan(QueryExpression value)
            => AddExpression(ComparisonExpr(value, ConditionOperator.GreaterThan));

        /// <summary>Добавить условие больше или равно.</summary>
        public TQuery IsGreaterOrEqual(object? value)
            => AddExpression(ComparisonExpr(value, ConditionOperator.GreaterThanOrEqual));

        /// <summary>Добавить условие меньше.</summary>
        public TQuery IsLess(object? value)
            => AddExpression(ComparisonExpr(value, ConditionOperator.LessThan));

        /// <summary>Добавить условие меньше с выражением.</summary>
        public TQuery IsLess(QueryExpression value)
            => AddExpression(ComparisonExpr(value, ConditionOperator.LessThan));

        /// <summary>Добавить условие меньше или равно.</summary>
        public TQuery IsLessOrEqual(object? value)
            => AddExpression(ComparisonExpr(value, ConditionOperator.LessThanOrEqual));

        /// <summary>
        /// Добавить условие LIKE. Шаблон со знаками <c>%</c> должен быть подготовлен вызывающим кодом.
        /// </summary>
        public TQuery IsLike(object? value)
        {
            var expr = ApplyNegation(ComparisonExpr(value, ConditionOperator.Like));
            _negated = false;
            return _addWhere(expr);
        }

        /// <summary>Добавить условие IS NULL. После <see cref="Not"/> формируется отрицание над IS NULL.</summary>
        public TQuery IsNull()
            => AddExpression(IsNullExpr());

        /// <summary>Добавить условие IN по подзапросу. После <see cref="Not"/> формируется NOT IN.</summary>
        public TQuery In(BaseQuery subQuery)
        {
            var op = _negated ? ConditionOperator.NotIn : ConditionOperator.In;
            return _addWhere(QueryExpression.Binary(
                LeftColumnExpr(),
                op,
                QueryExpression.SubQuery(subQuery)));
        }

        /// <summary>Добавить условие BETWEEN.</summary>
        public TQuery Between(object? low, object? high)
        {
            var expr = QueryExpression.And(
                ComparisonExpr(low, ConditionOperator.GreaterThanOrEqual),
                ComparisonExpr(high, ConditionOperator.LessThanOrEqual));

            return AddExpression(expr);
        }

        /// <summary>Обернуть произвольное выражение в NOT.</summary>
        public TQuery Not(QueryExpression expression)
        {
            return _addWhere(QueryExpression.Not(expression));
        }

        private QueryExpression LeftColumnExpr()
            => string.IsNullOrWhiteSpace(_alias)
                ? QueryExpression.Column(_columnName)
                : QueryExpression.Column(_alias, _columnName);

        private TQuery AddExpression(QueryExpression expression)
            => _addWhere(ApplyNegation(expression));

        private QueryExpression ApplyNegation(QueryExpression expression)
            => _negated ? QueryExpression.Not(expression) : expression;

        private QueryExpression ComparisonExpr(object? value, ConditionOperator op)
        {
            if (op == ConditionOperator.Equal && value is null)
            {
                return IsNullExpr();
            }

            var valueExpression = value is QueryExpression expression
                ? expression
                : QueryExpression.Param(value);

            return QueryExpression.Binary(LeftColumnExpr(), op, valueExpression);
        }

        private QueryExpression ComparisonExpr(string targetAlias, string targetColumnName, ConditionOperator op)
            => QueryExpression.Binary(
                LeftColumnExpr(),
                op,
                QueryExpression.Column(targetAlias, targetColumnName));

        private QueryExpression IsNullExpr()
            => string.IsNullOrWhiteSpace(_alias)
                ? QueryExpression.IsNull(_columnName)
                : QueryExpression.IsNull(_alias, _columnName);

    }
}
