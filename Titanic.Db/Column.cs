using Titanic.Db.Abstractions;

namespace Titanic.Db
{
    /// <summary>
    /// Точка доступа для создания SQL выражений колонок.
    /// Все методы делегируют QueryExpression (internal).
    /// </summary>
    public static class Column
    {
        // ===== Базовые =====
        /// <summary>Создать колонку.</summary>
        public static QueryExpression Name(string columnName) => QueryExpression.Column(columnName);
        /// <summary>Создать колонку с алиасом источника.</summary>
        public static QueryExpression Name(string alias, string columnName) => QueryExpression.Column(alias, columnName);
        /// <summary>Все колонки (*).</summary>
        public static QueryExpression Asterisk() => QueryExpression.Asterisk();
        /// <summary>Raw SQL фрагмент (только для доверенного SQL).</summary>
        public static QueryExpression Raw(string sql) => QueryExpression.Raw(sql);
        /// <summary>Параметр запроса (alias).</summary>
        public static QueryExpression Parameter(object? value) => QueryExpression.Param(value);
        /// <summary>Константа (литерал в SQL).</summary>
        public static QueryExpression Const(object? value) => QueryExpression.Const(value);
        /// <summary>Подзапрос как выражение колонки.</summary>
        public static QueryExpression SubQuery(Select query) => QueryExpression.SubQuery(query);
    }
}