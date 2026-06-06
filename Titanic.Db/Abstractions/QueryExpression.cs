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

        public ExpressionType ExpressionType { get; set; }

        /// <summary>SQL текст для raw выражений.</summary>
        public string? Sql { get; set; }

        /// <summary>Имя колонки.</summary>
        public string? ColumnName { get; set; }

        /// <summary>Алиас источника данных.</summary>
        public string? SourceAlias { get; set; }

        /// <summary>Алиас результата выражения.</summary>
        public string? Alias { get; set; }

        /// <summary>Значение параметра или константы.</summary>
        public object? Value { get; set; }

        /// <summary>SQL оператор или имя функции.</summary>
        /// <remarks>Используется для raw-выражений (когда <see cref="SqlFunctionType"/> = <see cref="Enums.SqlFunction.None"/> или <see cref="Enums.SqlFunction.Custom"/>) и для не-функциональных операторов (NOT, EXISTS, AND, OR).</remarks>
        public string? Operator { get; set; }

        /// <summary>Каноническая SQL-функция (рендерится движком).</summary>
        public SqlFunction SqlFunctionType { get; set; } = SqlFunction.None;

        /// <summary>Оператор условия.</summary>
        public ConditionOperator? ConditionOperatorType { get; set; }

        /// <summary>Подзапрос.</summary>
        public BaseQuery? Query { get; set; }

        /// <summary>Дочерние выражения.</summary>
        public IReadOnlyList<QueryExpression> Children => Expressions;

        #endregion Свойства

        #region Конструкторы

        /// <summary>Создать выражение колонки.</summary>
        public QueryExpression(string columnName)
        {
            ColumnName = columnName;
            ExpressionType = columnName == "*" ? ExpressionType.Asterisk : ExpressionType.SourceColumn;
        }

        /// <summary>Создать выражение с явным типом.</summary>
        public QueryExpression(string sql, ExpressionType type)
        {
            ExpressionType = type;
            if (type == ExpressionType.SourceColumn)
                ColumnName = sql;
            else
                Sql = sql;
        }

        /// <summary>Создать выражение колонки источника с алиасом.</summary>
        public QueryExpression(string alias, string columnName)
        {
            SourceAlias = alias;
            ColumnName = columnName;
            ExpressionType = columnName == "*" ? ExpressionType.Asterisk : ExpressionType.SourceColumn;
        }

        /// <summary>Создать выражение значения.</summary>
        public QueryExpression(object value, bool isParam = true)
        {
            Value = value;
            ExpressionType = isParam ? ExpressionType.Parameter : ExpressionType.Const;
        }

        private QueryExpression(ExpressionType expressionType, string? sql = null, string? op = null,
            ConditionOperator? conditionOperator = null, object? value = null, BaseQuery? query = null,
            IEnumerable<QueryExpression>? expressions = null)
        {
            ExpressionType = expressionType;
            Sql = sql;
            Operator = op;
            ConditionOperatorType = conditionOperator;
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
            => value == null ? IsNotNull(columnName) : Compare(columnName, ConditionOperator.NotEqual, Param(value));

        internal static QueryExpression NotEqual(string alias, string columnName, object? value)
            => value == null ? IsNotNull(alias, columnName) : Compare(alias, columnName, ConditionOperator.NotEqual, Param(value));

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
        internal static QueryExpression Contains(string columnName, object? value) => Compare(columnName, ConditionOperator.Contains, Param(value));
        internal static QueryExpression Contains(string alias, string columnName, object? value) => Compare(alias, columnName, ConditionOperator.Contains, Param(value));
        internal static QueryExpression Contains(string alias, string columnName, QueryExpression value) => Compare(alias, columnName, ConditionOperator.Contains, value);

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
            => new(ExpressionType.Group, op: "AND", expressions: expressions.Where(e => e != null));
        internal static QueryExpression Or(params QueryExpression[] expressions)
            => new(ExpressionType.Group, op: "OR", expressions: expressions.Where(e => e != null));
        internal static QueryExpression Not(QueryExpression expression)
            => new(ExpressionType.Unary, op: "NOT", expressions: new[] { expression });

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
            => new(ExpressionType.Unary, op: "EXISTS", expressions: new[] { SubQuery(query) });

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
        public string ToSql(QueryBuildContext context) => context.Engine.BuildExpression(this, context);

        public IEnumerator<QueryExpression> GetEnumerator() => Expressions.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion Методы
    }
}
