using Titanic.Db.Abstractions;

namespace Titanic.Db
{
    /// <summary>
    /// Запрос вставки данных через VALUES или INSERT ... SELECT.
    /// </summary>
    public class InsertSelect : BaseQuery
    {
        #region Поля

        /// <summary>
        /// Колонки INSERT.
        /// </summary>
        private readonly List<string> _columns = new();

        /// <summary>
        /// Строки VALUES.
        /// </summary>
        private readonly List<IReadOnlyList<QueryExpression>> _rows = new();

        /// <summary>
        /// Возвращаемые выражения.
        /// </summary>
        private readonly List<QueryExpression> _returning = new();

        /// <summary>
        /// Колонки конфликта ON CONFLICT.
        /// </summary>
        private readonly List<string> _conflictColumns = new();

        /// <summary>
        /// Значения UPDATE при ON CONFLICT DO UPDATE.
        /// </summary>
        private readonly List<SqlSet> _conflictUpdateSet = new();

        /// <summary>
        /// Имя таблицы вставки.
        /// </summary>
        private string? _tableName;

        /// <summary>
        /// SELECT источник для INSERT ... SELECT.
        /// </summary>
        private Select? _select;

        /// <summary>
        /// Raw SQL секция ON CONFLICT.
        /// </summary>
        private string? _conflictSql;

        /// <summary>
        /// Признак ON CONFLICT DO NOTHING.
        /// </summary>
        private bool _onConflictDoNothing;

        #endregion Поля

        #region Конструкторы

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="provider"> Провайдер БД. </param>
        /// <param name="engine"> Движок SQL-диалекта. </param>
        internal InsertSelect(BaseDbProvider? provider = null, BaseDbEngine? engine = null)
            : base(provider, engine)
        {
        }

        /// <summary>
        /// Конструктор с указанием таблицы вставки.
        /// </summary>
        /// <param name="tableName"> Имя таблицы. </param>
        internal InsertSelect(string tableName)
        {
            Into(tableName);
        }

        #endregion Конструкторы

        #region Методы построения запроса

        /// <summary>
        /// Установить таблицу вставки.
        /// </summary>
        /// <param name="tableName"> Имя таблицы. </param>
        public InsertSelect Into(string tableName)
        {
            _tableName = tableName;
            return this;
        }

        /// <summary>
        /// Установить колонки INSERT.
        /// </summary>
        /// <param name="columnNames"> Имена колонок. </param>
        public InsertSelect SetColumns(params string[] columnNames)
        {
            _columns.Clear();
            _columns.AddRange(columnNames);
            return this;
        }

        /// <summary>
        /// Добавить строку VALUES из значений параметров.
        /// </summary>
        /// <param name="values"> Значения строки. </param>
        public InsertSelect Values(params object?[] values)
        {
            var row = values.Select(QueryExpression.Param).ToList();
            _rows.Add(row);
            Expressions.AddRange(row);
            return this;
        }

        /// <summary>
        /// Добавить строку VALUES из SQL выражений.
        /// </summary>
        /// <param name="values"> SQL выражения строки. </param>
        public InsertSelect Values(params QueryExpression[] values)
        {
            _rows.Add(values);
            Expressions.AddRange(values);
            return this;
        }

        /// <summary>
        /// Добавить строку VALUES из словаря колонка-значение.
        /// </summary>
        /// <param name="values"> Значения по именам колонок. </param>
        public InsertSelect Values(IDictionary<string, object?> values)
        {
            if (_columns.Count == 0)
            {
                _columns.AddRange(values.Keys);
            }

            var row = _columns.Select(column => QueryExpression.Param(values.TryGetValue(column, out var value) ? value : null)).ToList();
            _rows.Add(row);
            Expressions.AddRange(row);
            return this;
        }

        /// <summary>
        /// Установить SELECT источник для INSERT ... SELECT.
        /// </summary>
        /// <param name="select"> SELECT запрос. </param>
        public InsertSelect FromSelect(Select select)
        {
            _select = select ?? throw new ArgumentNullException(nameof(select));
            return this;
        }

        /// <summary>
        /// Добавить возвращаемые колонки.
        /// </summary>
        /// <param name="columnNames"> Имена колонок. </param>
        public InsertSelect Returning(params string[] columnNames)
        {
            foreach (var columnName in columnNames)
            {
                _returning.Add(QueryExpression.Column(columnName));
            }

            return this;
        }

        /// <summary>
        /// Добавить возвращаемую колонку источника с алиасом.
        /// </summary>
        /// <param name="alias"> Алиас источника. </param>
        /// <param name="columnName"> Имя колонки. </param>
        public InsertSelect Returning(string alias, string columnName)
        {
            _returning.Add(QueryExpression.Column(alias, columnName));
            return this;
        }

        /// <summary>
        /// Добавить возвращаемое выражение.
        /// </summary>
        /// <param name="expression"> SQL выражение. </param>
        public InsertSelect Returning(QueryExpression expression)
        {
            _returning.Add(expression);
            Expressions.Add(expression);
            return this;
        }

        /// <summary>
        /// Добавить ON CONFLICT DO NOTHING.
        /// </summary>
        /// <param name="conflictColumns"> Колонки конфликта. </param>
        public InsertSelect OnConflictDoNothing(params string[] conflictColumns)
        {
            _onConflictDoNothing = true;
            _conflictColumns.Clear();
            _conflictColumns.AddRange(conflictColumns);
            _conflictUpdateSet.Clear();
            _conflictSql = null;
            return this;
        }

        /// <summary>
        /// Установить колонки конфликта для дальнейшего DO UPDATE.
        /// </summary>
        /// <param name="conflictColumns"> Колонки конфликта. </param>
        public InsertSelect OnConflict(params string[] conflictColumns)
        {
            _onConflictDoNothing = false;
            _conflictSql = null;
            _conflictColumns.Clear();
            _conflictColumns.AddRange(conflictColumns);
            return this;
        }

        /// <summary>
        /// Добавить SET для ON CONFLICT DO UPDATE.
        /// </summary>
        /// <param name="columnName"> Имя обновляемой колонки. </param>
        /// <param name="value"> Значение. </param>
        public InsertSelect DoUpdateSet(string columnName, QueryExpression value)
        {
            _onConflictDoNothing = false;
            _conflictSql = null;
            _conflictUpdateSet.Add(new SqlSet(Column.Name(columnName), value));
            return this;
        }

        /// <summary>
        /// Добавить SET для ON CONFLICT DO UPDATE из EXCLUDED колонки.
        /// </summary>
        /// <param name="columnName"> Имя обновляемой колонки. </param>
        public InsertSelect DoUpdateSetExcluded(string columnName)
        {
            return DoUpdateSet(columnName, Column.Name("excluded", columnName));
        }

        /// <summary>
        /// Добавить SET для ON CONFLICT DO UPDATE из EXCLUDED колонок.
        /// </summary>
        /// <param name="columnNames"> Имена обновляемых колонок. </param>
        public InsertSelect DoUpdateSetExcluded(params string[] columnNames)
        {
            foreach (var columnName in columnNames)
            {
                DoUpdateSetExcluded(columnName);
            }

            return this;
        }

        /// <summary>
        /// Добавить raw SQL секцию ON CONFLICT.
        /// </summary>
        /// <param name="sql"> SQL текст секции конфликта. </param>
        public InsertSelect OnConflictRaw(string sql)
        {
            _conflictSql = sql;
            _onConflictDoNothing = false;
            _conflictColumns.Clear();
            _conflictUpdateSet.Clear();
            return this;
        }

        #endregion Методы построения запроса

        #region Методы построения SQL

        /// <summary>
        /// Построить SQL запрос.
        /// </summary>
        /// <param name="context"> Контекст построения SQL. </param>
        protected internal override string BuildSql(QueryBuildContext context)
        {
            if (string.IsNullOrWhiteSpace(_tableName))
            {
                throw new InvalidOperationException("Insert table is not specified");
            }

            if (_select == null && _rows.Count == 0)
            {
                throw new InvalidOperationException("Insert source is not specified");
            }

            return context.Engine.BuildInsertSql(context, new SqlInsertParts(
                _tableName,
                _columns,
                _rows,
                _select,
                _returning,
                _onConflictDoNothing,
                _conflictColumns,
                _conflictUpdateSet,
                _conflictSql));
        }

        #endregion Методы построения SQL
    }
}
