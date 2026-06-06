using Titanic.Db.Abstractions;

namespace Titanic.Db.Builders
{
    /// <summary>
    /// Билдер для выбора колонок SELECT.
    /// Позволяет добавлять колонки, DISTINCT и завершить через From().
    /// </summary>
    public class ColumnItem
    {
        private readonly Select _select;
        private readonly List<QueryExpression> _columns = new();
        private bool _distinct;

        internal ColumnItem(Select select)
        {
            _select = select;
        }

        /// <summary>
        /// Добавить колонку SELECT.
        /// </summary>
        public ColumnItem Column(string columnName, string? alias = null)
        {
            _columns.Add(QueryExpression.Column(columnName).AsIfNotEmpty(alias));
            return this;
        }

        /// <summary>
        /// Добавить колонку источника с алиасом.
        /// </summary>
        public ColumnItem Column(string sourceAlias, string columnName, string? alias = null)
        {
            _columns.Add(QueryExpression.Column(sourceAlias, columnName).AsIfNotEmpty(alias));
            return this;
        }

        /// <summary>
        /// Добавить выражение SELECT.
        /// </summary>
        public ColumnItem Column(QueryExpression expression, string? alias = null)
        {
            if (!string.IsNullOrWhiteSpace(alias))
                expression.As(alias);
            _columns.Add(expression);
            return this;
        }

        /// <summary>
        /// Добавить выражение колонки с алиасом таблицы.
        /// </summary>
        public ColumnItem Column(string sourceAlias, QueryExpression expression)
        {
            if (expression.ExpressionType == Enums.ExpressionType.Asterisk)
                expression.SourceAlias = sourceAlias;
            _columns.Add(expression);
            return this;
        }

        /// <summary>
        /// Задать алиас для последней добавленной колонки.
        /// </summary>
        public ColumnItem As(string alias)
        {
            if (_columns.Count > 0)
            {
                var last = _columns[^1];
                last.As(alias);
            }
            return this;
        }

        /// <summary>
        /// Добавить DISTINCT.
        /// </summary>
        public ColumnItem Distinct()
        {
            _distinct = true;
            return this;
        }

        /// <summary>
        /// Установить источник FROM и завершить контекст колонок.
        /// </summary>
        public FromItem From(string tableName)
        {
            foreach (var column in _columns)
                _select.Column(column);

            if (_distinct)
                _select.Distinct();

            return new FromItem(_select, tableName);
        }
    }
}