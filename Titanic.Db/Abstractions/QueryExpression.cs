using System.Collections;
using Titanic.Db.Enums;

namespace Titanic.Db.Abstractions
{
    /// <summary>
    /// Выражение запроса.
    /// </summary>
    public class QueryExpression : IEnumerable<QueryExpression>
    {
        #region Свойства

        protected List<QueryExpression> Expressions = new();

        /// <summary>
        /// Тип выражения, по которому SQL-движок выбирает правило генерации SQL.
        /// </summary>
        public ExpressionType ExpressionType { get; set; }

        /// <summary>
        /// SQL-фрагмент, который используется для выражений с явно заданным raw-текстом.
        /// </summary>
        public string? Sql { get; set; }

        /// <summary>
        /// Имя колонки источника данных, участвующей в выражении.
        /// </summary>
        public string? ColumnName { get; set; }

        /// <summary>
        /// Алиас таблицы, подзапроса или другого источника, которому принадлежит колонка.
        /// </summary>
        public string? SourceAlias { get; set; }

        /// <summary>
        /// Алиас, под которым результат выражения будет доступен во внешнем SQL.
        /// </summary>
        public string? Alias { get; set; }

        /// <summary>
        /// Значение параметра или константы, которое будет передано в SQL через параметры запроса.
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// SQL-оператор или имя функции, необходимое для выражений с настраиваемой генерацией.
        /// </summary>
        /// <remarks>
        /// Используется для raw-выражений, бинарных операторов, а также custom SQL-функций.
        /// </remarks>
        public string? Operator { get; set; }

        /// <summary>
        /// Тип унарного оператора, который должен быть применён к текущему выражению.
        /// </summary>
        public UnaryOperator UnaryOperatorType { get; set; } = UnaryOperator.None;

        /// <summary>
        /// Каноническая SQL-функция, которую конкретный движок преобразует в диалект базы данных.
        /// </summary>
        public SqlFunction SqlFunctionType { get; set; } = SqlFunction.None;

        /// <summary>
        /// Оператор условия, который определяет сравнение между дочерними выражениями.
        /// </summary>
        public ConditionOperator? ConditionOperatorType { get; set; }

        /// <summary>
        /// Логический оператор, который объединяет дочерние выражения группы.
        /// </summary>
        public LogicalOperator? LogicalOperatorType { get; set; }

        /// <summary>
        /// Подзапрос, используемый выражением для операций вроде <c>IN</c> или вложенных выборок.
        /// </summary>
        public BaseQuery? Query { get; set; }

        /// <summary>
        /// Дочерние выражения, из которых строится составное SQL-выражение.
        /// </summary>
        public IReadOnlyList<QueryExpression> Children => Expressions;

        #endregion Свойства

        #region Конструкторы

        /// <summary>
        /// Создать выражение колонки по её имени.
        /// </summary>
        /// <param name="columnName">Имя колонки или символ <c>*</c> для выбора всех колонок.</param>
        public QueryExpression(string columnName)
        {
            ColumnName = columnName;
            ExpressionType = columnName == "*" ? ExpressionType.Asterisk : ExpressionType.SourceColumn;
        }

        /// <summary>
        /// Создать выражение из SQL-фрагмента с явно указанным типом выражения.
        /// </summary>
        /// <param name="sql">SQL-фрагмент или имя колонки, в зависимости от указанного типа.</param>
        /// <param name="type">Тип создаваемого выражения.</param>
        public QueryExpression(string sql, ExpressionType type)
        {
            ExpressionType = type;
            if (type == ExpressionType.SourceColumn)
                ColumnName = sql;
            else
                Sql = sql;
        }

        /// <summary>
        /// Создать выражение колонки с указанием алиаса источника данных.
        /// </summary>
        /// <param name="alias">Алиас таблицы или подзапроса, которому принадлежит колонка.</param>
        /// <param name="columnName">Имя колонки или символ <c>*</c> для выбора всех колонок источника.</param>
        public QueryExpression(string alias, string columnName)
        {
            SourceAlias = alias;
            ColumnName = columnName;
            ExpressionType = columnName == "*" ? ExpressionType.Asterisk : ExpressionType.SourceColumn;
        }

        /// <summary>
        /// Создать выражение параметра или константного значения.
        /// </summary>
        /// <param name="value">Значение, которое будет сохранено в выражении.</param>
        /// <param name="isParam">Если <c>true</c>, значение будет параметром запроса; иначе будет SQL-константой.</param>
        public QueryExpression(object value, bool isParam = true)
        {
            Value = value;
            ExpressionType = isParam ? ExpressionType.Parameter : ExpressionType.Const;
        }

        private QueryExpression(ExpressionType expressionType, string? sql = null, string? op = null,
            ConditionOperator? conditionOperator = null, UnaryOperator unaryOperator = UnaryOperator.None,
            LogicalOperator? logicalOperator = null, object? value = null, BaseQuery? query = null,
            IEnumerable<QueryExpression>? expressions = null)
        {
            ExpressionType = expressionType;
            Sql = sql;
            Operator = op;
            ConditionOperatorType = conditionOperator;
            UnaryOperatorType = unaryOperator;
            LogicalOperatorType = logicalOperator;
            Value = value;
            Query = query;
            if (expressions != null)
                Expressions.AddRange(expressions);
        }

        #endregion Конструкторы

        #region Фабричные методы (internal — доступны только через Column, Func, Select и т.д.)

        internal static QueryExpression Asterisk() => new("*", ExpressionType.Asterisk);
        internal static QueryExpression Raw(string sql) => new(sql, ExpressionType.SqlText);
        internal static QueryExpression Column(string columnName) => new(columnName);
        internal static QueryExpression Column(string sourceAlias, string columnName) => new(sourceAlias, columnName);
        internal static QueryExpression Column(string sourceAlias, QueryExpression column)
        {
            if (column.ExpressionType != ExpressionType.SourceColumn && column.ExpressionType != ExpressionType.Asterisk)
                throw new ArgumentException("Expression must be a column or asterisk", nameof(column));
            column.SourceAlias = sourceAlias;
            return column;
        }

        internal static QueryExpression Param(object? value) => new(value!);
        internal static QueryExpression Const(object? value) => new(value!, false);

        internal static QueryExpression Binary(QueryExpression left, ConditionOperator op, QueryExpression right)
            => new(ExpressionType.Binary, conditionOperator: op, expressions: new[] { left, right });

        internal static QueryExpression Binary(QueryExpression left, string op, QueryExpression right)
            => new(ExpressionType.Binary, op: op, expressions: new[] { left, right });

        internal static QueryExpression Compare(string columnName, ConditionOperator op, QueryExpression value)
            => Binary(Column(columnName), op, value);

        internal static QueryExpression Compare(string alias, string columnName, ConditionOperator op, QueryExpression value)
            => Binary(Column(alias, columnName), op, value);

        internal static QueryExpression Equal(string columnName, object? value)
            => value == null ? IsNull(columnName) : Compare(columnName, ConditionOperator.Equal, Param(value));

        internal static QueryExpression Equal(string alias, string columnName, object? value)
            => value == null ? IsNull(alias, columnName) : Compare(alias, columnName, ConditionOperator.Equal, Param(value));

        internal static QueryExpression Equal(string alias, string columnName, QueryExpression value)
            => Binary(Column(alias, columnName), ConditionOperator.Equal, value);

        internal static QueryExpression Equal(string columnName, QueryExpression value)
            => Binary(Column(columnName), ConditionOperator.Equal, value);

        internal static QueryExpression NotEqual(string columnName, object? value)
            => value == null ? Not(IsNull(columnName)) : Compare(columnName, ConditionOperator.NotEqual, Param(value));

        internal static QueryExpression NotEqual(string alias, string columnName, object? value)
            => value == null ? Not(IsNull(alias, columnName)) : Compare(alias, columnName, ConditionOperator.NotEqual, Param(value));

        internal static QueryExpression Greater(string columnName, object? value) => Compare(columnName, ConditionOperator.GreaterThan, Param(value));
        internal static QueryExpression Greater(string alias, string columnName, object? value) => Compare(alias, columnName, ConditionOperator.GreaterThan, Param(value));
        internal static QueryExpression GreaterOrEqual(string columnName, object? value) => Compare(columnName, ConditionOperator.GreaterThanOrEqual, Param(value));
        internal static QueryExpression GreaterOrEqual(string alias, string columnName, object? value) => Compare(alias, columnName, ConditionOperator.GreaterThanOrEqual, Param(value));
        internal static QueryExpression GreaterOrEqual(string alias, string columnName, QueryExpression value) => Compare(alias, columnName, ConditionOperator.GreaterThanOrEqual, value);
        internal static QueryExpression Less(string columnName, object? value) => Compare(columnName, ConditionOperator.LessThan, Param(value));
        internal static QueryExpression Less(string alias, string columnName, object? value) => Compare(alias, columnName, ConditionOperator.LessThan, Param(value));
        internal static QueryExpression Less(string alias, string columnName, QueryExpression value) => Compare(alias, columnName, ConditionOperator.LessThan, value);
        internal static QueryExpression LessOrEqual(string columnName, object? value) => Compare(columnName, ConditionOperator.LessThanOrEqual, Param(value));
        internal static QueryExpression LessOrEqual(string alias, string columnName, object? value) => Compare(alias, columnName, ConditionOperator.LessThanOrEqual, Param(value));
        internal static QueryExpression Like(string columnName, object? value) => Compare(columnName, ConditionOperator.Like, Param(value));
        internal static QueryExpression Like(string alias, string columnName, object? value) => Compare(alias, columnName, ConditionOperator.Like, Param(value));
        internal static QueryExpression Like(string alias, string columnName, QueryExpression value) => Compare(alias, columnName, ConditionOperator.Like, value);

        internal static QueryExpression IsNull(string columnName)
            => new(ExpressionType.Unary, conditionOperator: ConditionOperator.IsNull, expressions: new[] { Column(columnName) });
        internal static QueryExpression IsNull(string alias, string columnName)
            => new(ExpressionType.Unary, conditionOperator: ConditionOperator.IsNull, expressions: new[] { Column(alias, columnName) });
        internal static QueryExpression IsNotNull(string columnName)
            => new(ExpressionType.Unary, conditionOperator: ConditionOperator.IsNotNull, expressions: new[] { Column(columnName) });
        internal static QueryExpression IsNotNull(string alias, string columnName)
            => new(ExpressionType.Unary, conditionOperator: ConditionOperator.IsNotNull, expressions: new[] { Column(alias, columnName) });

        internal static QueryExpression In(string columnName, IEnumerable<object?> values)
            => new(ExpressionType.Binary, conditionOperator: ConditionOperator.In, expressions: new[] { Column(columnName), List(values.Select(Param)) });
        internal static QueryExpression In(string alias, string columnName, IEnumerable<object?> values)
            => new(ExpressionType.Binary, conditionOperator: ConditionOperator.In, expressions: new[] { Column(alias, columnName), List(values.Select(Param)) });
        internal static QueryExpression NotIn(string columnName, IEnumerable<object?> values)
            => new(ExpressionType.Binary, conditionOperator: ConditionOperator.NotIn, expressions: new[] { Column(columnName), List(values.Select(Param)) });
        internal static QueryExpression NotIn(string alias, string columnName, IEnumerable<object?> values)
            => new(ExpressionType.Binary, conditionOperator: ConditionOperator.NotIn, expressions: new[] { Column(alias, columnName), List(values.Select(Param)) });

        internal static QueryExpression And(params QueryExpression[] expressions)
        {
            return new(
                ExpressionType.Group,
                logicalOperator: LogicalOperator.And,
                expressions: expressions.Where(e => e != null));
        }

        internal static QueryExpression Or(params QueryExpression[] expressions)
        {
            return new(
                ExpressionType.Group,
                logicalOperator: LogicalOperator.Or,
                expressions: expressions.Where(e => e != null));
        }
        internal static QueryExpression Not(QueryExpression expression)
            => new(ExpressionType.Unary, unaryOperator: UnaryOperator.Not, expressions: new[] { expression });

        internal static QueryExpression List(IEnumerable<QueryExpression> expressions)
            => new(ExpressionType.List, expressions: expressions);

        internal static QueryExpression Function(string functionName, params QueryExpression[] arguments)
            => new(ExpressionType.Function, op: functionName, expressions: arguments)
            {
                SqlFunctionType = SqlFunction.Custom
            };

        /// <summary>
        /// Создать выражение канонической SQL-функции (рендерится движком).
        /// Имя функции НЕ должно хардкодиться строкой — используйте <see cref="Enums.SqlFunction"/>.
        /// </summary>
        internal static QueryExpression Function(SqlFunction function, params QueryExpression[] arguments)
            => new(ExpressionType.Function, expressions: arguments)
            {
                SqlFunctionType = function
            };
        internal static QueryExpression SubQuery(BaseQuery query)
            => new(ExpressionType.SubQuery, query: query);
        internal static QueryExpression SubQuery(Select query)
            => new(ExpressionType.SubQuery, query: query);
        internal static QueryExpression Exists(BaseQuery query)
            => new(ExpressionType.Unary, unaryOperator: UnaryOperator.Exists, expressions: new[] { SubQuery(query) });

        internal static QueryExpression In(string columnName, BaseQuery subQuery)
            => new(ExpressionType.Binary, conditionOperator: ConditionOperator.In, expressions: new[] { Column(columnName), SubQuery(subQuery) });
        internal static QueryExpression NotIn(string columnName, BaseQuery subQuery)
            => new(ExpressionType.Binary, conditionOperator: ConditionOperator.NotIn, expressions: new[] { Column(columnName), SubQuery(subQuery) });

        internal static QueryExpression Case(QueryExpression whenExpression, QueryExpression thenExpression, QueryExpression elseExpression)
            => new(ExpressionType.Case, expressions: new[] { whenExpression, thenExpression, elseExpression });

        internal static QueryExpression Case(IEnumerable<(QueryExpression When, QueryExpression Then)> branches, QueryExpression elseExpression)
        {
            var branchList = branches?.ToList() ?? throw new ArgumentNullException(nameof(branches));
            if (branchList.Count == 0)
                throw new ArgumentException("CASE branches are empty", nameof(branches));

            var expressions = new List<QueryExpression>(branchList.Count * 2 + 1);
            foreach (var (whenExpression, thenExpression) in branchList)
            {
                expressions.Add(whenExpression);
                expressions.Add(thenExpression);
            }

            expressions.Add(elseExpression);
            return new(ExpressionType.Case, expressions: expressions);
        }

        /// <summary>
        /// Установить алиас выражения.
        /// </summary>
        public QueryExpression As(string alias)
        {
            Alias = alias;
            return this;
        }

        #endregion Фабричные методы

        #region Методы

        /// <summary>
        /// Построить SQL выражение.
        /// </summary>
        /// <param name="context">Контекст построения SQL, содержащий движок диалекта и параметры.</param>
        /// <returns>SQL-фрагмент, соответствующий текущему выражению.</returns>
        public string ToSql(QueryBuildContext context)
        {
            return context.Engine.BuildExpression(this, context);
        }

        /// <summary>
        /// Перечислить дочерние выражения текущего узла.
        /// </summary>
        /// <returns>Перечислитель дочерних выражений.</returns>
        public IEnumerator<QueryExpression> GetEnumerator()
        {
            return Expressions.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion Методы
    }
}
