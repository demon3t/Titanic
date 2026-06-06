using Titanic.Db.Abstractions;
using Titanic.Db.Builders;

namespace Titanic.Db
{
    /// <summary>
    /// Запрос на выборку.
    /// </summary>
    public class Select : BaseQuery
    {
        #region Поля

        private readonly List<QueryExpression> _columns = new();
        private readonly List<SqlJoin> _joins = new();
        private readonly List<QueryExpression> _groupBy = new();
        private readonly List<SqlOrderBy> _orderBy = new();
        private readonly List<SqlUnion> _unions = new();
        private string? _from;
        private string? _fromAlias;
        private QueryExpression? _where;
        private QueryExpression? _having;
        private bool _distinct;
        private int? _limit;
        private int? _offset;

        #endregion Поля

        #region Конструкторы

        /// <summary>
        /// Конструктор SELECT-запроса для внутреннего использования провайдером/движком.
        /// </summary>
        /// <param name="provider">Провайдер БД.</param>
        /// <param name="engine">SQL-движок.</param>
        internal Select(BaseDbProvider? provider = null, BaseDbEngine? engine = null)
            : base(provider, engine) { }

        /// <summary>
        /// Конструктор SELECT-запроса с первичным набором колонок.
        /// </summary>
        /// <param name="columns">Имена колонок.</param>
        internal Select(params string[] columns)
        {
            Columns(columns);
        }

        #endregion Конструкторы

        #region Column Builder

        /// <summary>
        /// Добавить колонку SELECT. Возвращает ColumnItem с методом As().
        /// </summary>
        public Builders.ColumnItem Column(string columnName)
        {
            _columns.Add(QueryExpression.Column(columnName));
            return new Builders.ColumnItem(this);
        }

        /// <summary>
        /// Добавить колонку источника с алиасом. Возвращает ColumnItem с методом As().
        /// </summary>
        public Builders.ColumnItem Column(string sourceAlias, string columnName)
        {
            _columns.Add(QueryExpression.Column(sourceAlias, columnName));
            return new Builders.ColumnItem(this);
        }

        /// <summary>
        /// Добавить выражение SELECT. Возвращает ColumnItem с методом As().
        /// </summary>
        public Builders.ColumnItem Column(QueryExpression expression)
        {
            _columns.Add(expression);
            return new Builders.ColumnItem(this);
        }

        /// <summary>
        /// Добавить выражение колонки с алиасом таблицы. Возвращает ColumnItem с методом As().
        /// </summary>
        public Builders.ColumnItem Column(string sourceAlias, QueryExpression expression)
        {
            if (expression.ExpressionType == Enums.ExpressionType.Asterisk)
                expression.SourceAlias = sourceAlias;
            _columns.Add(expression);
            return new Builders.ColumnItem(this);
        }

        /// <summary>
        /// Внутренний метод для добавления колонки из ColumnItem.
        /// </summary>
        internal Select AddColumn(QueryExpression expression)
        {
            _columns.Add(expression);
            Expressions.Add(expression);
            return this;
        }

        /// <summary>
        /// Добавить DISTINCT.
        /// </summary>
        public Select Distinct()
        {
            _distinct = true;
            return this;
        }

        /// <summary>
        /// Добавить колонки SELECT.
        /// </summary>
        public Select Columns(params string[] columnNames)
        {
            foreach (var columnName in columnNames)
            {
                _columns.Add(QueryExpression.Column(columnName));
            }

            return this;
        }

        #endregion Column Builder

        #region From

        /// <summary>
        /// Установить источник FROM. Возвращает FromItem с методом As().
        /// </summary>
        public Builders.FromItem From(string tableName)
        {
            return new Builders.FromItem(this, tableName);
        }

        /// <summary>
        /// Установить источник FROM с алиасом (internal, для ORM/Item-билдеров).
        /// </summary>
        internal Select SetFrom(string tableName, string? alias = null)
        {
            _from = tableName;
            _fromAlias = alias;
            return this;
        }

        #endregion From

        #region Join Builder

        /// <summary>
        /// Создать JOIN-билдер.
        /// </summary>
        public Builders.JoinItem Join(string tableName, JoinType joinType = JoinType.Inner)
        {
            return new Builders.JoinItem(this, tableName, joinType);
        }

        /// <summary>
        /// Создать LEFT JOIN-билдер.
        /// </summary>
        public Builders.JoinItem LeftJoin(string tableName) => Join(tableName, JoinType.Left);

        /// <summary>
        /// Создать RIGHT JOIN-билдер.
        /// </summary>
        public Builders.JoinItem RightJoin(string tableName) => Join(tableName, JoinType.Right);

        /// <summary>
        /// Создать FULL JOIN-билдер.
        /// </summary>
        public Builders.JoinItem FullJoin(string tableName) => Join(tableName, JoinType.Full);

        /// <summary>
        /// Создать CROSS JOIN-билдер.
        /// </summary>
        public Builders.JoinItem CrossJoin(string tableName) => Join(tableName, JoinType.Cross);

        /// <summary>
        /// Создать INNER JOIN-билдер.
        /// </summary>
        public Builders.JoinItem InnerJoin(string tableName) => Join(tableName, JoinType.Inner);

        #endregion Join Builder

        #region Where

        /// <summary>
        /// Установить условие WHERE.
        /// </summary>
        public Select Where(QueryExpression expression)
        {
            _where = expression;
            Expressions.Add(expression);
            return this;
        }

        /// <summary>
        /// WHERE по колонке без указания алиаса (например: Where("is_active").IsEqual(...)).
        /// </summary>
        public Builders.WhereItem<Select> Where(string columnName)
            => new(this, string.Empty, columnName, expr =>
            {
                _where = _where == null ? expr : QueryExpression.And(_where, expr);
                Expressions.Add(expr);
                return this;
            });

        /// <summary>
        /// Установить raw SQL условие WHERE.
        /// </summary>
        public Select WhereRaw(string rawSql) => Where(QueryExpression.Raw(rawSql));

        /// <summary>
        /// Создать билдер условий WHERE.
        /// </summary>
        public WhereBuilder<Select> Where() => new(this, Where);

        /// <summary>
        /// WHERE с указанием алиаса и колонки.
        /// </summary>
        public Builders.WhereItem<Select> Where(string alias, string columnName)
            => new(this, alias, columnName, expr => { _where = _where == null ? expr : QueryExpression.And(_where, expr); Expressions.Add(expr); return this; });

        /// <summary>
        /// AND с указанием алиаса и колонки.
        /// </summary>
        public Builders.WhereItem<Select> And(string alias, string columnName)
            => Where(alias, columnName);

        /// <summary>
        /// AND по колонке без указания алиаса.
        /// </summary>
        public Builders.WhereItem<Select> And(string columnName)
            => Where(columnName);

        /// <summary>
        /// OR с указанием алиаса и колонки.
        /// </summary>
        public Builders.WhereItem<Select> Or(string alias, string columnName)
            => new(this, alias, columnName, expr => { _where = _where == null ? expr : QueryExpression.Or(_where, expr); Expressions.Add(expr); return this; });

        /// <summary>
        /// OR по колонке без указания алиаса.
        /// </summary>
        public Builders.WhereItem<Select> Or(string columnName)
            => new(this, string.Empty, columnName, expr =>
            {
                _where = _where == null ? expr : QueryExpression.Or(_where, expr);
                Expressions.Add(expr);
                return this;
            });

        /// <summary>
        /// Добавить условие WHERE EXISTS (подзапрос).
        /// </summary>
        public Select Exists(BaseQuery subQuery) => Where(QueryExpression.Exists(subQuery));

        #endregion Where

        #region GroupBy

        /// <summary>
        /// Добавить колонки GROUP BY.
        /// </summary>
        public Select GroupBy(params string[] columnNames)
        {
            _groupBy.AddRange(columnNames.Select(QueryExpression.Column));
            return this;
        }

        /// <summary>
        /// Добавить колонку источника с алиасом в GROUP BY.
        /// </summary>
        public Select GroupBy(string sourceAlias, string columnName)
        {
            _groupBy.Add(QueryExpression.Column(sourceAlias, columnName));
            return this;
        }

        #endregion GroupBy

        #region Having

        /// <summary>
        /// Установить готовое условие HAVING (например, <c>QueryExpression.Raw("count(*) > 1")</c>).
        /// Для fluent-сравнения агрегата используйте <see cref="Having(QueryExpression)"/>.
        /// </summary>
        /// <param name="expression">Готовое условие (например, результат <c>QueryExpression.Binary</c>).</param>
        public Select HavingCondition(QueryExpression expression)
        {
            _having = expression;
            Expressions.Add(expression);
            return this;
        }

        /// <summary>
        /// Начать построение условия HAVING из агрегатного выражения.
        /// Возвращает <see cref="Builders.HavingExpression"/>, у которого есть fluent-методы <c>Is*</c>.
        /// </summary>
        /// <param name="aggregate">Агрегатное выражение, например <c>Func.Count("ep", "project_id")</c>.</param>
        public Builders.HavingExpression Having(QueryExpression aggregate)
        {
            return new Builders.HavingExpression(this, aggregate);
        }

        #endregion Having

        #region Limit/Offset Builder

        /// <summary>
        /// Создать билдер LIMIT/OFFSET с указанием LIMIT.
        /// </summary>
        public Builders.PagingItem Limit(int limit)
        {
            return new Builders.PagingItem(this).Limit(limit);
        }

        #endregion Limit/Offset Builder

        #region OrderBy

        /// <summary>
        /// Добавить сортировку по колонке.
        /// </summary>
        public Select OrderBy(string columnName, bool desc = false)
        {
            _orderBy.Add(new SqlOrderBy(QueryExpression.Column(columnName), desc));
            return this;
        }

        /// <summary>
        /// Добавить сортировку по колонке источника с алиасом.
        /// </summary>
        public Select OrderBy(string sourceAlias, string columnName, bool desc = false)
        {
            _orderBy.Add(new SqlOrderBy(QueryExpression.Column(sourceAlias, columnName), desc));
            return this;
        }

        /// <summary>
        /// Добавить сортировку по выражению.
        /// </summary>
        public Select OrderBy(QueryExpression expression, bool desc = false)
        {
            _orderBy.Add(new SqlOrderBy(expression, desc));
            Expressions.Add(expression);
            return this;
        }

        /// <summary>
        /// Добавить сортировку по убыванию.
        /// </summary>
        public Select OrderByDescending(string columnName) => OrderBy(columnName, true);

        /// <summary>
        /// Добавить дополнительную сортировку по возрастанию.
        /// </summary>
        public Select ThenBy(string columnName) => OrderBy(columnName);

        /// <summary>
        /// Добавить дополнительную сортировку по убыванию.
        /// </summary>
        public Select ThenByDescending(string columnName) => OrderBy(columnName, true);

        #endregion OrderBy


        #region Union

        /// <summary>
        /// Добавить UNION.
        /// </summary>
        public Select Union(Select select)
        {
            _unions.Add(new SqlUnion(select, false));
            return this;
        }

        /// <summary>
        /// Добавить UNION ALL.
        /// </summary>
        public Select UnionAll(Select select)
        {
            _unions.Add(new SqlUnion(select, true));
            return this;
        }

        #endregion Union

        #region SQL

        protected internal override string BuildSql(QueryBuildContext context)
        {
            return context.Engine.BuildSelectSql(context, new SqlSelectParts(
                _columns.Select(column => new SqlSelectColumn(column)).ToList(),
                _from,
                _fromAlias,
                _joins,
                _where,
                _groupBy,
                _having,
                _orderBy,
                _limit,
                _offset,
                _distinct,
                _unions));
        }

        #endregion SQL

        #region Internal (backward compatibility)

        internal Select AddJoin(string tableName, QueryExpression on, string? alias = null, JoinType joinType = JoinType.Inner)
        {
            _joins.Add(new SqlJoin(joinType, tableName, alias, on));
            Expressions.Add(on);
            return this;
        }

        internal Select OnInternal(QueryExpression expression)
        {
            if (_joins.Count == 0)
            {
                throw new InvalidOperationException("JOIN is not specified");
            }

            var last = _joins[^1];
            _joins[^1] = last with { On = expression };
            Expressions.Add(expression);
            return this;
        }

        internal Select LimitInternal(int limit)
        {
            if (limit < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(limit));
            }

            _limit = limit;
            return this;
        }

        internal Select OffsetInternal(int offset)
        {
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            _offset = offset;
            return this;
        }

        #endregion Internal
    }
}
