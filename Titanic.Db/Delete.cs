using Titanic.Db.Abstractions;
using Titanic.Db.Builders;
namespace Titanic.Db
{
    /// <summary>
    /// Запрос на удаление данных.
    /// </summary>
    public class Delete : BaseQuery
    {
        #region Поля

        private readonly List<SqlTable> _using = new();
        private readonly List<QueryExpression> _returning = new();
        private string? _tableName;
        private string? _alias;
        private QueryExpression? _where;

        #endregion Поля

        #region Конструкторы

        /// <summary>
        /// Конструктор DELETE-запроса для внутреннего использования провайдером/движком.
        /// </summary>
        /// <param name="provider">Провайдер БД.</param>
        /// <param name="engine">SQL-движок.</param>
        internal Delete(BaseDbProvider? provider = null, BaseDbEngine? engine = null)
            : base(provider, engine)
        {
        }

        /// <summary>
        /// Конструктор DELETE-запроса с указанием таблицы удаления.
        /// </summary>
        /// <param name="tableName">Имя таблицы.</param>
        internal Delete(string tableName)
        {
            From(tableName);
        }

        #endregion Конструкторы

        #region Методы построения запроса

        /// <summary>
        /// Установить таблицу удаления (DELETE FROM) и необязательный алиас.
        /// </summary>
        /// <param name="tableName">Имя таблицы.</param>
        /// <param name="alias">Алиас таблицы.</param>
        public Delete From(string tableName, string? alias = null)
        {
            _tableName = tableName;
            _alias = alias;
            return this;
        }

        /// <summary>
        /// Установить алиас для таблицы удаления.
        /// </summary>
        /// <param name="alias">Алиас таблицы.</param>
        public Delete As(string alias)
        {
            _alias = alias;
            return this;
        }

        /// <summary>
        /// Добавить таблицу в секцию USING.
        /// </summary>
        /// <param name="tableName">Имя таблицы.</param>
        /// <param name="alias">Алиас таблицы.</param>
        public Delete Using(string tableName, string? alias = null)
        {
            _using.Add(new SqlTable(tableName, alias));
            return this;
        }

        /// <summary>
        /// Установить выражение WHERE.
        /// </summary>
        /// <param name="expression">Условие удаления.</param>
        public Delete Where(QueryExpression expression)
        {
            _where = expression;
            Expressions.Add(expression);
            return this;
        }

        /// <summary>
        /// Установить raw SQL условие WHERE.
        /// </summary>
        /// <param name="rawSql">SQL-фрагмент условия.</param>
        public Delete Where(string rawSql) => Where(QueryExpression.Raw(rawSql));

        /// <summary>
        /// Внутренний билдер условий WHERE.
        /// </summary>
        public WhereBuilder<Delete> Where() => new(this, Where);

        /// <summary>
        /// WHERE с указанием алиаса и колонки.
        /// </summary>
        public Builders.WhereItem<Delete> Where(string alias, string columnName)
            => new(this, alias, columnName, expr => { _where = _where == null ? expr : QueryExpression.And(_where, expr); Expressions.Add(expr); return this; });

        /// <summary>
        /// AND с указанием алиаса и колонки.
        /// </summary>
        public Builders.WhereItem<Delete> And(string alias, string columnName)
            => Where(alias, columnName);

        /// <summary>
        /// OR с указанием алиаса и колонки.
        /// </summary>
        public Builders.WhereItem<Delete> Or(string alias, string columnName)
            => new(this, alias, columnName, expr => { _where = _where == null ? expr : QueryExpression.Or(_where, expr); Expressions.Add(expr); return this; });

        /// <summary>
        /// Добавить условие WHERE EXISTS.
        /// </summary>
        /// <param name="subQuery">Подзапрос EXISTS.</param>
        public Delete Exists(BaseQuery subQuery) => Where(QueryExpression.Exists(subQuery));

        /// <summary>
        /// Добавить колонки в секцию RETURNING.
        /// </summary>
        /// <param name="columnNames">Имена колонок.</param>
        public Delete Returning(params string[] columnNames)
        {
            foreach (var columnName in columnNames)
            {
                _returning.Add(QueryExpression.Column(columnName));
            }
            return this;
        }

        /// <summary>
        /// Добавить колонку с алиасом в секцию RETURNING.
        /// </summary>
        /// <param name="alias">Алиас источника.</param>
        /// <param name="columnName">Имя колонки.</param>
        public Delete Returning(string alias, string columnName)
        {
            _returning.Add(QueryExpression.Column(alias, columnName));
            return this;
        }

        /// <summary>
        /// Добавить выражение в секцию RETURNING.
        /// </summary>
        /// <param name="expression">SQL-выражение.</param>
        public Delete Returning(QueryExpression expression)
        {
            _returning.Add(expression);
            Expressions.Add(expression);
            return this;
        }

        #endregion Методы построения запроса

        #region Методы построения SQL

        /// <summary>
        /// Построить SQL DELETE-запрос.
        /// </summary>
        /// <param name="context">Контекст построения SQL.</param>
        protected internal override string BuildSql(QueryBuildContext context)
        {
            if (string.IsNullOrWhiteSpace(_tableName))
                throw new InvalidOperationException("Delete table is not specified");

            return context.Engine.BuildDeleteSql(context, new SqlDeleteParts(_tableName, _alias, _using, _where, _returning));
        }

        #endregion Методы построения SQL
    }
}
