using Titanic.Db.Abstractions;

namespace Titanic.Db.Builders
{
    /// <summary>
    /// Билдер для построения условий WHERE.
    /// Все методы сравнения начинаются с Is.
    /// Для отрицания вызывайте Not() перед Is*.
    /// </summary>
    /// <typeparam name="TQuery">Тип запроса (Select, Delete, Update).</typeparam>
    public class WhereItem<TQuery> where TQuery : BaseQuery
    {
        private readonly TQuery _query;
        private readonly string _alias;
        private readonly string _columnName;
        private readonly Func<QueryExpression, TQuery> _addWhere;
        private bool _negated;

        internal WhereItem(TQuery query, string alias, string columnName, Func<QueryExpression, TQuery> addWhere)
        {
            _query = query;
            _alias = alias;
            _columnName = columnName;
            _addWhere = addWhere;
        }

        /// <summary>
        /// Включить отрицание для следующего условия Is*.
        /// </summary>
        public WhereItem<TQuery> Not()
        {
            _negated = true;
            return this;
        }

        /// <summary>
        /// Добавить условие равенства.
        /// </summary>
        public TQuery IsEqual(object? value)
        {
            var expr = _negated
                ? QueryExpression.Not(EqualExpr(value))
                : EqualExpr(value);
            return _addWhere(expr);
        }

        /// <summary>
        /// Добавить условие равенства с колонкой другого источника.
        /// </summary>
        public TQuery IsEqual(string targetAlias, string targetColumnName)
        {
            var expr = QueryExpression.Binary(
                LeftColumnExpr(),
                Enums.ConditionOperator.Equal,
                QueryExpression.Column(targetAlias, targetColumnName));
            if (_negated)
                expr = QueryExpression.Not(expr);
            return _addWhere(expr);
        }

        /// <summary>
        /// Добавить условие равенства с выражением.
        /// </summary>
        public TQuery IsEqual(QueryExpression value)
        {
            var expr = QueryExpression.Binary(
                LeftColumnExpr(),
                Enums.ConditionOperator.Equal,
                value);
            if (_negated)
                expr = QueryExpression.Not(expr);
            return _addWhere(expr);
        }

        /// <summary>
        /// Добавить условие больше.
        /// </summary>
        public TQuery IsGreaterThan(object? value)
        {
            var expr = _negated
                ? QueryExpression.Not(GreaterExpr(value))
                : GreaterExpr(value);
            return _addWhere(expr);
        }

        /// <summary>
        /// Добавить условие больше с выражением.
        /// </summary>
        public TQuery IsGreaterThan(QueryExpression value)
        {
            var expr = QueryExpression.Binary(
                LeftColumnExpr(),
                Enums.ConditionOperator.GreaterThan,
                value);
            if (_negated)
                expr = QueryExpression.Not(expr);
            return _addWhere(expr);
        }

        /// <summary>
        /// Добавить условие больше или равно.
        /// </summary>
        public TQuery IsGreaterOrEqual(object? value)
        {
            var expr = _negated
                ? QueryExpression.Not(GreaterOrEqualExpr(value))
                : GreaterOrEqualExpr(value);
            return _addWhere(expr);
        }

        /// <summary>
        /// Добавить условие меньше.
        /// </summary>
        public TQuery IsLess(object? value)
        {
            var expr = _negated
                ? QueryExpression.Not(LessExpr(value))
                : LessExpr(value);
            return _addWhere(expr);
        }

        /// <summary>
        /// Добавить условие меньше с выражением.
        /// </summary>
        public TQuery IsLess(QueryExpression value)
        {
            var expr = QueryExpression.Binary(
                LeftColumnExpr(),
                Enums.ConditionOperator.LessThan,
                value);
            if (_negated)
                expr = QueryExpression.Not(expr);
            return _addWhere(expr);
        }

        /// <summary>
        /// Добавить условие меньше или равно.
        /// </summary>
        public TQuery IsLessOrEqual(object? value)
        {
            var expr = _negated
                ? QueryExpression.Not(LessOrEqualExpr(value))
                : LessOrEqualExpr(value);
            return _addWhere(expr);
        }

        /// <summary>
        /// Добавить условие LIKE (или NOT LIKE если вызван Not()).
        /// </summary>
        public TQuery IsLike(object? value)
        {
            if (_negated)
            {
                return _addWhere(QueryExpression.Binary(
                    LeftColumnExpr(),
                    Enums.ConditionOperator.NotLike,
                    QueryExpression.Param(value)));
            }
            return _addWhere(LikeExpr(value));
        }

        /// <summary>
        /// Добавить условие IS NULL (или IS NOT NULL если вызван Not()).
        /// </summary>
        public TQuery IsNull()
        {
            if (_negated)
                return _addWhere(IsNotNullExpr());
            return _addWhere(IsNullExpr());
        }

        /// <summary>
        /// Добавить условие IS NOT NULL.
        /// </summary>
        public TQuery IsNotNull()
        {
            if (_negated)
                return _addWhere(IsNullExpr());
            return _addWhere(IsNotNullExpr());
        }

        /// <summary>
        /// Добавить условие IN (подзапрос). Если вызван Not() — NOT IN.
        /// </summary>
        public TQuery In(BaseQuery subQuery)
        {
            var op = _negated ? Enums.ConditionOperator.NotIn : Enums.ConditionOperator.In;
            return _addWhere(QueryExpression.Binary(
                LeftColumnExpr(),
                op,
                QueryExpression.SubQuery(subQuery)));
        }

        /// <summary>
        /// Добавить условие BETWEEN.
        /// </summary>
        public TQuery Between(object? low, object? high)
        {
            var expr = QueryExpression.And(
                GreaterOrEqualExpr(low),
                LessOrEqualExpr(high));
            if (_negated)
                expr = QueryExpression.Not(expr);
            return _addWhere(expr);
        }

        /// <summary>
        /// NOT: оборачивает выражение в NOT.
        /// </summary>
        public TQuery Not(QueryExpression expression)
        {
            return _addWhere(QueryExpression.Not(expression));
        }

        private QueryExpression LeftColumnExpr()
            => string.IsNullOrWhiteSpace(_alias)
                ? QueryExpression.Column(_columnName)
                : QueryExpression.Column(_alias, _columnName);

        private QueryExpression EqualExpr(object? value)
        {
            if (value is QueryExpression valueExpression)
                return QueryExpression.Binary(LeftColumnExpr(), Enums.ConditionOperator.Equal, valueExpression);

            return string.IsNullOrWhiteSpace(_alias)
                ? QueryExpression.Equal(_columnName, value)
                : QueryExpression.Equal(_alias, _columnName, value);
        }

        private QueryExpression GreaterExpr(object? value)
        {
            if (value is QueryExpression valueExpression)
                return QueryExpression.Binary(LeftColumnExpr(), Enums.ConditionOperator.GreaterThan, valueExpression);

            return string.IsNullOrWhiteSpace(_alias)
                ? QueryExpression.Greater(_columnName, value)
                : QueryExpression.Greater(_alias, _columnName, value);
        }

        private QueryExpression GreaterOrEqualExpr(object? value)
        {
            if (value is QueryExpression valueExpression)
                return QueryExpression.Binary(LeftColumnExpr(), Enums.ConditionOperator.GreaterThanOrEqual, valueExpression);

            return string.IsNullOrWhiteSpace(_alias)
                ? QueryExpression.GreaterOrEqual(_columnName, value)
                : QueryExpression.GreaterOrEqual(_alias, _columnName, value);
        }

        private QueryExpression LessExpr(object? value)
        {
            if (value is QueryExpression valueExpression)
                return QueryExpression.Binary(LeftColumnExpr(), Enums.ConditionOperator.LessThan, valueExpression);

            return string.IsNullOrWhiteSpace(_alias)
                ? QueryExpression.Less(_columnName, value)
                : QueryExpression.Less(_alias, _columnName, value);
        }

        private QueryExpression LessOrEqualExpr(object? value)
        {
            if (value is QueryExpression valueExpression)
                return QueryExpression.Binary(LeftColumnExpr(), Enums.ConditionOperator.LessThanOrEqual, valueExpression);

            return string.IsNullOrWhiteSpace(_alias)
                ? QueryExpression.LessOrEqual(_columnName, value)
                : QueryExpression.LessOrEqual(_alias, _columnName, value);
        }

        private QueryExpression LikeExpr(object? value)
        {
            if (value is QueryExpression valueExpression)
                return QueryExpression.Binary(LeftColumnExpr(), Enums.ConditionOperator.Like, valueExpression);

            return string.IsNullOrWhiteSpace(_alias)
                ? QueryExpression.Like(_columnName, value)
                : QueryExpression.Like(_alias, _columnName, value);
        }

        private QueryExpression IsNullExpr()
            => string.IsNullOrWhiteSpace(_alias)
                ? QueryExpression.IsNull(_columnName)
                : QueryExpression.IsNull(_alias, _columnName);

        private QueryExpression IsNotNullExpr()
            => string.IsNullOrWhiteSpace(_alias)
                ? QueryExpression.IsNotNull(_columnName)
                : QueryExpression.IsNotNull(_alias, _columnName);
    }
}