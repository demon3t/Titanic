using Titanic.Db.Abstractions;
using Titanic.Db.Builders;

namespace Titanic.Db
{
    /// <summary>
    /// Запрос на обновление данных.
    /// </summary>
    public class Update : BaseQuery
    {
        #region Поля

        private readonly List<SqlSet> _set = new();
        private readonly List<SqlTable> _from = new();
        private readonly List<QueryExpression> _returning = new();
        private string? _tableName;
        private string? _alias;
        private QueryExpression? _where;

        #endregion Поля

        #region Конструкторы

        /// <summary>
        /// Конструктор UPDATE-запроса для внутреннего использования провайдером/движком.
        /// </summary>
        /// <param name="provider">Провайдер БД.</param>
        /// <param name="engine">SQL-движок.</param>
        internal Update(BaseDbProvider? provider = null, BaseDbEngine? engine = null)
            : base(provider, engine) { }

        /// <summary>
        /// Конструктор UPDATE-запроса с указанием таблицы.
        /// </summary>
        /// <param name="tableName">Имя таблицы.</param>
        internal Update(string tableName) { Table(tableName); }

        #endregion Конструкторы

        #region Методы построения запроса

        /// <summary>
        /// Установить таблицу UPDATE и необязательный алиас.
        /// </summary>
        /// <param name="tableName">Имя таблицы.</param>
        /// <param name="alias">Алиас таблицы.</param>
        public Update Table(string tableName, string? alias = null)
        {
            _tableName = tableName;
            _alias = alias;
            return this;
        }

        /// <summary>
        /// Задать алиас для таблицы UPDATE.
        /// </summary>
        public Update As(string alias)
        {
            _alias = alias;
            return this;
        }

        /// <summary>
        /// Добавить значение в секцию SET.
        /// </summary>
        /// <param name="columnName">Имя колонки.</param>
        /// <param name="value">Выражение значения.</param>
        public Update Set(string columnName, QueryExpression value)
        {
            _set.Add(new SqlSet(QueryExpression.Column(columnName), value));
            Expressions.Add(value);
            return this;
        }

        /// <summary>
        /// Добавить значение в секцию SET с указанием алиаса источника.
        /// </summary>
        /// <param name="alias">Алиас источника.</param>
        /// <param name="columnName">Имя колонки.</param>
        /// <param name="value">Выражение значения.</param>
        public Update Set(string alias, string columnName, QueryExpression value)
        {
            _set.Add(new SqlSet(QueryExpression.Column(alias, columnName), value));
            Expressions.Add(value);
            return this;
        }

        /// <summary>
        /// Добавить параметризованное значение в секцию SET.
        /// </summary>
        /// <param name="columnName">Имя колонки.</param>
        /// <param name="value">Значение параметра.</param>
        public Update Set(string columnName, object? value) => Set(columnName, QueryExpression.Param(value));

        /// <summary>
        /// Добавить выражение в секцию SET.
        /// </summary>
        /// <param name="columnName">Имя колонки.</param>
        /// <param name="expression">SQL-выражение.</param>
        public Update SetExpression(string columnName, QueryExpression expression) => Set(columnName, expression);

        /// <summary>
        /// Добавить таблицу в секцию FROM.
        /// </summary>
        /// <param name="tableName">Имя таблицы.</param>
        /// <param name="alias">Алиас таблицы.</param>
        public Update From(string tableName, string? alias = null)
        {
            _from.Add(new SqlTable(tableName, alias));
            return this;
        }

        /// <summary>
        /// Установить выражение WHERE.
        /// </summary>
        /// <param name="expression">Условие обновления.</param>
        public Update Where(QueryExpression expression)
        {
            _where = expression;
            Expressions.Add(expression);
            return this;
        }

        /// <summary>
        /// Установить raw SQL условие WHERE.
        /// </summary>
        /// <param name="rawSql">SQL-фрагмент условия.</param>
        public Update Where(string rawSql) => Where(QueryExpression.Raw(rawSql));

        /// <summary>
        /// Внутренний билдер условий WHERE.
        /// </summary>
        public WhereBuilder<Update> Where() => new(this, Where);

        /// <summary>
        /// WHERE с указанием алиаса и колонки.
        /// </summary>
        public Builders.WhereItem<Update> Where(string alias, string columnName)
            => new(this, alias, columnName, expr => { _where = _where == null ? expr : QueryExpression.And(_where, expr); Expressions.Add(expr); return this; });

        /// <summary>
        /// AND с указанием алиаса и колонки.
        /// </summary>
        public Builders.WhereItem<Update> And(string alias, string columnName)
            => Where(alias, columnName);

        /// <summary>
        /// OR с указанием алиаса и колонки.
        /// </summary>
        public Builders.WhereItem<Update> Or(string alias, string columnName)
            => new(this, alias, columnName, expr => { _where = _where == null ? expr : QueryExpression.Or(_where, expr); Expressions.Add(expr); return this; });

        /// <summary>
        /// Добавить условие WHERE EXISTS.
        /// </summary>
        /// <param name="subQuery">Подзапрос EXISTS.</param>
        public Update Exists(BaseQuery subQuery) => Where(QueryExpression.Exists(subQuery));

        /// <summary>
        /// Добавить колонки в секцию RETURNING.
        /// </summary>
        /// <param name="columnNames">Имена колонок.</param>
        public Update Returning(params string[] columnNames)
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
        public Update Returning(string alias, string columnName)
        {
            _returning.Add(QueryExpression.Column(alias, columnName));
            return this;
        }

        /// <summary>
        /// Добавить выражение в секцию RETURNING.
        /// </summary>
        /// <param name="expression">SQL-выражение.</param>
        public Update Returning(QueryExpression expression)
        {
            _returning.Add(expression);
            Expressions.Add(expression);
            return this;
        }

        #endregion Методы построения запроса

        #region Методы построения SQL

        /// <summary>
        /// Построить SQL UPDATE-запрос.
        /// </summary>
        /// <param name="context">Контекст построения SQL.</param>
        protected internal override string BuildSql(QueryBuildContext context)
        {
            if (string.IsNullOrWhiteSpace(_tableName))
                throw new InvalidOperationException("Update table is not specified");
            if (_set.Count == 0)
                throw new InvalidOperationException("Update SET values are not specified");

            return context.Engine.BuildUpdateSql(context, new SqlUpdateParts(_tableName, _alias, _set, _from, _where, _returning));
        }

        #endregion Методы построения SQL
    }
}
