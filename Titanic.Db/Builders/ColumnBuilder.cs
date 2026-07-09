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
        /// Adds a column to the SELECT list.
        /// </summary>
        /// <param name="columnName">The column name.</param>
        /// <returns>The current column builder.</returns>
        public ColumnBuilder Column(string columnName)
        {
            _select.AddColumn(QueryExpression.Column(columnName));
            return this;
        }

        /// <summary>
        /// Adds a column from the specified source alias to the SELECT list.
        /// </summary>
        /// <param name="sourceAlias">The source alias.</param>
        /// <param name="columnName">The column name.</param>
        /// <returns>The current column builder.</returns>
        public ColumnBuilder Column(string sourceAlias, string columnName)
        {
            _select.AddColumn(QueryExpression.Column(sourceAlias, columnName));
            return this;
        }

        /// <summary>
        /// Adds an expression to the SELECT list.
        /// </summary>
        /// <param name="expression">The expression to select.</param>
        /// <returns>The current column builder.</returns>
        public ColumnBuilder Column(QueryExpression expression)
        {
            _select.AddColumn(expression);
            return this;
        }

        /// <summary>
        /// Adds an expression associated with the specified source alias to the SELECT list.
        /// </summary>
        /// <param name="sourceAlias">The source alias.</param>
        /// <param name="expression">The expression to select.</param>
        /// <returns>The current column builder.</returns>
        public ColumnBuilder Column(string sourceAlias, QueryExpression expression)
        {
            _select.AddColumn(expression);
            return this;
        }

        /// <summary>
        /// Marks the SELECT query as DISTINCT.
        /// </summary>
        /// <returns>The configured SELECT query.</returns>
        public Select Distinct()
        {
            _select.Distinct();
            return _select;
        }

        /// <summary>
        /// Sets the source table for the SELECT query.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        /// <param name="alias">The optional table alias.</param>
        /// <returns>The configured SELECT query.</returns>
        public Select From(string tableName, string? alias = null)
        {
            _select.SetFrom(tableName, alias);
            return _select;
        }
    }
}
