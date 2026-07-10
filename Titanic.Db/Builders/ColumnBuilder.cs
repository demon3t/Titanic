using Titanic.Db.Abstractions;

namespace Titanic.Db.Builders
{
    /// <summary>
    /// Внутренний билдер колонок SELECT.
    /// </summary>
    internal class ColumnBuilder
    {
        private readonly Select _select;

        internal ColumnBuilder(Select select)
        {
            _select = select;
        }

        /// <summary>
        /// Добавить колонку в список выбираемых выражений SELECT.
        /// </summary>
        /// <param name="columnName">Имя колонки без алиаса источника.</param>
        /// <returns>Текущий билдер колонок для продолжения настройки SELECT.</returns>
        public ColumnBuilder Column(string columnName)
        {
            _select.AddColumn(QueryExpression.Column(columnName));
            return this;
        }

        /// <summary>
        /// Добавить колонку из указанного источника в список выбираемых выражений SELECT.
        /// </summary>
        /// <param name="sourceAlias">Алиас таблицы или подзапроса, которому принадлежит колонка.</param>
        /// <param name="columnName">Имя выбираемой колонки.</param>
        /// <returns>Текущий билдер колонок для продолжения настройки SELECT.</returns>
        public ColumnBuilder Column(string sourceAlias, string columnName)
        {
            _select.AddColumn(QueryExpression.Column(sourceAlias, columnName));
            return this;
        }

        /// <summary>
        /// Добавить готовое SQL-выражение в список выбираемых выражений SELECT.
        /// </summary>
        /// <param name="expression">Выражение, которое будет добавлено в секцию SELECT.</param>
        /// <returns>Текущий билдер колонок для продолжения настройки SELECT.</returns>
        public ColumnBuilder Column(QueryExpression expression)
        {
            _select.AddColumn(expression);
            return this;
        }

        /// <summary>
        /// Добавить готовое SQL-выражение, логически связанное с указанным алиасом источника.
        /// </summary>
        /// <param name="sourceAlias">Алиас источника данных, к которому относится выражение.</param>
        /// <param name="expression">Выражение, которое будет добавлено в секцию SELECT.</param>
        /// <returns>Текущий билдер колонок для продолжения настройки SELECT.</returns>
        public ColumnBuilder Column(string sourceAlias, QueryExpression expression)
        {
            _select.AddColumn(expression);
            return this;
        }

        /// <summary>
        /// Включить режим DISTINCT для текущего SELECT-запроса.
        /// </summary>
        /// <returns>Настраиваемый SELECT-запрос для продолжения цепочки вызовов.</returns>
        public Select Distinct()
        {
            _select.Distinct();
            return _select;
        }

        /// <summary>
        /// Указать таблицу-источник для текущего SELECT-запроса.
        /// </summary>
        /// <param name="tableName">Имя таблицы, из которой будет выполняться выборка.</param>
        /// <param name="alias">Необязательный алиас таблицы в SQL-запросе.</param>
        /// <returns>Настраиваемый SELECT-запрос для продолжения цепочки вызовов.</returns>
        public Select From(string tableName, string? alias = null)
        {
            _select.SetFrom(tableName, alias);
            return _select;
        }
    }
}
