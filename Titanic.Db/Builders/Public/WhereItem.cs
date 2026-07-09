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
        /// Включить отрицание для следующего условия, сформированного этим билдером.
        /// </summary>
        /// <returns>Текущий билдер условия для продолжения fluent-цепочки.</returns>
        public WhereItem<TQuery> Not()
        {
            _negated = true;
            return this;
        }

        /// <summary>
        /// Добавить в запрос условие равенства текущей колонки переданному значению.
        /// Значение <see langword="null"/> автоматически преобразуется в условие <c>IS NULL</c>.
        /// </summary>
        /// <param name="value">Значение, с которым сравнивается текущая колонка.</param>
        /// <returns>Запрос, в который добавлено условие.</returns>
        public TQuery IsEqual(object? value)
        {
            return AddExpression(ComparisonExpr(value, ConditionOperator.Equal));
        }

        /// <summary>
        /// Добавить в запрос условие равенства текущей колонки другой колонке.
        /// </summary>
        /// <param name="targetAlias">Алиас таблицы или подзапроса, которому принадлежит сравниваемая колонка.</param>
        /// <param name="targetColumnName">Имя колонки, с которой сравнивается текущая колонка.</param>
        /// <returns>Запрос, в который добавлено условие.</returns>
        public TQuery IsEqual(string targetAlias, string targetColumnName)
        {
            return AddExpression(ComparisonExpr(targetAlias, targetColumnName, ConditionOperator.Equal));
        }

        /// <summary>
        /// Добавить в запрос условие равенства текущей колонки произвольному SQL-выражению.
        /// </summary>
        /// <param name="value">Выражение, с которым сравнивается текущая колонка.</param>
        /// <returns>Запрос, в который добавлено условие.</returns>
        public TQuery IsEqual(QueryExpression value)
        {
            return AddExpression(ComparisonExpr(value, ConditionOperator.Equal));
        }

        /// <summary>
        /// Добавить в запрос условие, проверяющее что текущая колонка больше переданного значения.
        /// </summary>
        /// <param name="value">Значение, с которым сравнивается текущая колонка.</param>
        /// <returns>Запрос, в который добавлено условие.</returns>
        public TQuery IsGreaterThan(object? value)
        {
            return AddExpression(ComparisonExpr(value, ConditionOperator.GreaterThan));
        }

        /// <summary>
        /// Добавить в запрос условие, проверяющее что текущая колонка больше результата SQL-выражения.
        /// </summary>
        /// <param name="value">Выражение, с которым сравнивается текущая колонка.</param>
        /// <returns>Запрос, в который добавлено условие.</returns>
        public TQuery IsGreaterThan(QueryExpression value)
        {
            return AddExpression(ComparisonExpr(value, ConditionOperator.GreaterThan));
        }

        /// <summary>
        /// Добавить в запрос условие, проверяющее что текущая колонка больше или равна переданному значению.
        /// </summary>
        /// <param name="value">Значение, с которым сравнивается текущая колонка.</param>
        /// <returns>Запрос, в который добавлено условие.</returns>
        public TQuery IsGreaterOrEqual(object? value)
        {
            return AddExpression(ComparisonExpr(value, ConditionOperator.GreaterThanOrEqual));
        }

        /// <summary>
        /// Добавить в запрос условие, проверяющее что текущая колонка меньше переданного значения.
        /// </summary>
        /// <param name="value">Значение, с которым сравнивается текущая колонка.</param>
        /// <returns>Запрос, в который добавлено условие.</returns>
        public TQuery IsLess(object? value)
        {
            return AddExpression(ComparisonExpr(value, ConditionOperator.LessThan));
        }

        /// <summary>
        /// Добавить в запрос условие, проверяющее что текущая колонка меньше результата SQL-выражения.
        /// </summary>
        /// <param name="value">Выражение, с которым сравнивается текущая колонка.</param>
        /// <returns>Запрос, в который добавлено условие.</returns>
        public TQuery IsLess(QueryExpression value)
        {
            return AddExpression(ComparisonExpr(value, ConditionOperator.LessThan));
        }

        /// <summary>
        /// Добавить в запрос условие, проверяющее что текущая колонка меньше или равна переданному значению.
        /// </summary>
        /// <param name="value">Значение, с которым сравнивается текущая колонка.</param>
        /// <returns>Запрос, в который добавлено условие.</returns>
        public TQuery IsLessOrEqual(object? value)
        {
            return AddExpression(ComparisonExpr(value, ConditionOperator.LessThanOrEqual));
        }

        /// <summary>
        /// Добавить в запрос условие <c>LIKE</c> для текущей колонки.
        /// Шаблон со знаками <c>%</c> должен быть подготовлен вызывающим кодом.
        /// </summary>
        /// <param name="value">Шаблон, с которым сравнивается текущая колонка.</param>
        /// <returns>Запрос, в который добавлено условие.</returns>
        public TQuery IsLike(object? value)
        {
            var expr = ApplyNegation(ComparisonExpr(value, ConditionOperator.Like));
            _negated = false;
            return _addWhere(expr);
        }

        /// <summary>
        /// Добавить в запрос условие <c>IS NULL</c> для текущей колонки.
        /// После вызова <see cref="Not"/> условие будет обёрнуто в отрицание.
        /// </summary>
        /// <returns>Запрос, в который добавлено условие.</returns>
        public TQuery IsNull()
        {
            return AddExpression(IsNullExpr());
        }

        /// <summary>
        /// Добавить в запрос условие <c>IN</c>, где набор значений берётся из подзапроса.
        /// После вызова <see cref="Not"/> оператор будет заменён на <c>NOT IN</c>.
        /// </summary>
        /// <param name="subQuery">Подзапрос, возвращающий значения для сравнения.</param>
        /// <returns>Запрос, в который добавлено условие.</returns>
        public TQuery In(BaseQuery subQuery)
        {
            var op = _negated ? ConditionOperator.NotIn : ConditionOperator.In;
            return _addWhere(QueryExpression.Binary(
                LeftColumnExpr(),
                op,
                QueryExpression.SubQuery(subQuery)));
        }

        /// <summary>
        /// Добавить в запрос условие диапазона для текущей колонки.
        /// Границы диапазона включаются через сравнения <c>&gt;=</c> и <c>&lt;=</c>.
        /// </summary>
        /// <param name="low">Нижняя граница диапазона.</param>
        /// <param name="high">Верхняя граница диапазона.</param>
        /// <returns>Запрос, в который добавлено условие.</returns>
        public TQuery Between(object? low, object? high)
        {
            var expr = QueryExpression.And(
                ComparisonExpr(low, ConditionOperator.GreaterThanOrEqual),
                ComparisonExpr(high, ConditionOperator.LessThanOrEqual));

            return AddExpression(expr);
        }

        /// <summary>
        /// Добавить в запрос произвольное выражение, обёрнутое в оператор <c>NOT</c>.
        /// </summary>
        /// <param name="expression">Выражение, которое нужно отрицать.</param>
        /// <returns>Запрос, в который добавлено отрицательное условие.</returns>
        public TQuery Not(QueryExpression expression)
        {
            return _addWhere(QueryExpression.Not(expression));
        }

        private QueryExpression LeftColumnExpr()
        {
            return string.IsNullOrWhiteSpace(_alias)
                ? QueryExpression.Column(_columnName)
                : QueryExpression.Column(_alias, _columnName);
        }

        private TQuery AddExpression(QueryExpression expression)
        {
            return _addWhere(ApplyNegation(expression));
        }

        private QueryExpression ApplyNegation(QueryExpression expression)
        {
            return _negated ? QueryExpression.Not(expression) : expression;
        }

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
        {
            return QueryExpression.Binary(
                LeftColumnExpr(),
                op,
                QueryExpression.Column(targetAlias, targetColumnName));
        }

        private QueryExpression IsNullExpr()
        {
            return string.IsNullOrWhiteSpace(_alias)
                ? QueryExpression.IsNull(_columnName)
                : QueryExpression.IsNull(_alias, _columnName);
        }

    }
}
