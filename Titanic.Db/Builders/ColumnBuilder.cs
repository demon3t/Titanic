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

        public ColumnBuilder Column(string columnName)
        {
            _select.AddColumn(QueryExpression.Column(columnName));
            return this;
        }

        public ColumnBuilder Column(string sourceAlias, string columnName)
        {
            _select.AddColumn(QueryExpression.Column(sourceAlias, columnName));
            return this;
        }

        public ColumnBuilder Column(QueryExpression expression)
        {
            _select.AddColumn(expression);
            return this;
        }

        public ColumnBuilder Column(string sourceAlias, QueryExpression expression)
        {
            _select.AddColumn(expression);
            return this;
        }

        public Select Distinct()
        {
            _select.Distinct();
            return _select;
        }

        public Select From(string tableName, string? alias = null)
        {
            _select.SetFrom(tableName, alias);
            return _select;
        }
    }
}