using Titanic.Db.Abstractions;
using Titanic.Db.Builders;
using Titanic.Db.Enums;

namespace Titanic.Db
{
    /// <summary>
    /// Фабрика часто используемых SQL выражений и функций.
    /// Имена SQL-функций инкапсулированы в <see cref="SqlFunction"/> и рендерятся движком.
    /// </summary>
    public static class Func
    {
        #region SQL функции

        /// <summary>
        /// Создать функцию COUNT.
        /// </summary>
        /// <param name="expression">Аргумент. Для <c>COUNT(*)</c> используйте <c>Column.Asterisk()</c>.</param>
        public static QueryExpression Count(QueryExpression expression)
            => QueryExpression.Function(SqlFunction.Count, expression);

        /// <summary>
        /// Создать функцию COUNT(column).
        /// </summary>
        public static QueryExpression Count(string columnName)
            => QueryExpression.Function(SqlFunction.Count, QueryExpression.Column(columnName));

        /// <summary>
        /// Создать функцию COUNT(alias.column).
        /// </summary>
        public static QueryExpression Count(string alias, string columnName)
            => QueryExpression.Function(SqlFunction.Count, QueryExpression.Column(alias, columnName));

        /// <summary>
        /// Создать функцию SUM.
        /// </summary>
        public static QueryExpression Sum(QueryExpression expression)
            => QueryExpression.Function(SqlFunction.Sum, expression);

        /// <summary>
        /// Создать функцию SUM(alias.column).
        /// </summary>
        public static QueryExpression Sum(string alias, string columnName)
            => QueryExpression.Function(SqlFunction.Sum, QueryExpression.Column(alias, columnName));

        /// <summary>
        /// Создать функцию MIN.
        /// </summary>
        public static QueryExpression Min(QueryExpression expression)
            => QueryExpression.Function(SqlFunction.Min, expression);

        /// <summary>
        /// Создать функцию MIN с алиасом и колонкой.
        /// </summary>
        public static QueryExpression Min(string alias, string column)
            => QueryExpression.Function(SqlFunction.Min, QueryExpression.Column(alias, column));

        /// <summary>
        /// Создать функцию MAX.
        /// </summary>
        public static QueryExpression Max(QueryExpression expression)
            => QueryExpression.Function(SqlFunction.Max, expression);

        /// <summary>
        /// Создать функцию MAX с алиасом и колонкой.
        /// </summary>
        public static QueryExpression Max(string alias, string column)
            => QueryExpression.Function(SqlFunction.Max, QueryExpression.Column(alias, column));

        /// <summary>
        /// Создать функцию AVG.
        /// </summary>
        public static QueryExpression Avg(QueryExpression expression)
            => QueryExpression.Function(SqlFunction.Avg, expression);

        /// <summary>
        /// Создать функцию AVG с алиасом и колонкой.
        /// </summary>
        public static QueryExpression Avg(string alias, string column)
            => QueryExpression.Function(SqlFunction.Avg, QueryExpression.Column(alias, column));

        /// <summary>
        /// Создать функцию LOWER.
        /// </summary>
        public static QueryExpression Lower(QueryExpression expression)
            => QueryExpression.Function(SqlFunction.Lower, expression);

        /// <summary>
        /// Создать функцию UPPER.
        /// </summary>
        public static QueryExpression Upper(QueryExpression expression)
            => QueryExpression.Function(SqlFunction.Upper, expression);

        /// <summary>
        /// Создать функцию COALESCE.
        /// </summary>
        public static QueryExpression Coalesce(params QueryExpression[] expressions)
            => QueryExpression.Function(SqlFunction.Coalesce, expressions);

        /// <summary>
        /// Создать функцию COALLISE. Оставлено как alias к Coalesce.
        /// </summary>
        public static QueryExpression Coallise(params QueryExpression[] expressions)
            => Coalesce(expressions);

        /// <summary>
        /// Создать fluent-билдер для сложного CASE выражения.
        /// </summary>
        public static CaseItem Case() => new();

        /// <summary>
        /// Создать CASE WHEN expression THEN thenExpression ELSE elseExpression END.
        /// </summary>
        public static QueryExpression Case(QueryExpression whenExpression, QueryExpression thenExpression, QueryExpression elseExpression)
        {
            return QueryExpression.Case(whenExpression, thenExpression, elseExpression);
        }

        /// <summary>
        /// Создать raw SQL функцию (имя передаётся как строка).
        /// Использовать только для функций, не описанных в <see cref="SqlFunction"/>.
        /// </summary>
        public static QueryExpression Custom(string functionName, params QueryExpression[] expressions)
        {
            if (string.IsNullOrWhiteSpace(functionName))
                throw new ArgumentException("Custom function name is empty", nameof(functionName));

            return QueryExpression.Function(functionName, expressions);
        }

        #endregion SQL функции
    }
}
