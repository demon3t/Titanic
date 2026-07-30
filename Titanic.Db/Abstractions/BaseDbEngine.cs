using System.Text;
using Titanic.Db.Enums;

namespace Titanic.Db.Abstractions
{
    /// <summary>
    /// Движок SQL-диалекта конкретной БД.
    /// Отвечает за синтаксис целевой БД: идентификаторы, параметры, литералы.
    /// </summary>
    public abstract class BaseDbEngine
    {
        #region Свойства

        /// <summary>
        /// Тип БД.
        /// </summary>
        public abstract DatabaseType DatabaseType { get; }

        /// <summary>
        /// Префикс параметров в SQL тексте.
        /// </summary>
        public virtual string ParameterPrefix => "@";

        /// <summary>
        /// Отступ SQL секций.
        /// </summary>
        public virtual string Indent => "\t";

        #endregion Свойства

        #region Методы параметров и идентификаторов

        /// <summary>
        /// Создать имя параметра без префикса.
        /// </summary>
        /// <param name="index"> Порядковый номер параметра. </param>
        public virtual string BuildParameterName(int index) => $"p{index}";

        /// <summary>
        /// Получить SQL placeholder параметра.
        /// </summary>
        /// <param name="parameterName"> Имя параметра без префикса. </param>
        public virtual string GetParameterPlaceholder(string parameterName) => $"{ParameterPrefix}{parameterName}";

        /// <summary>
        /// Экранировать один SQL идентификатор.
        /// </summary>
        /// <param name="identifier"> Имя таблицы, колонки, алиаса или схемы. </param>
        public abstract string QuoteIdentifier(string identifier);

        /// <summary>
        /// Экранировать составной идентификатор: schema.table, alias.column, alias.*.
        /// </summary>
        /// <param name="identifier"> Составной идентификатор. </param>
        public virtual string QuoteQualifiedIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                throw new ArgumentException("Identifier is empty", nameof(identifier));
            }

            if (identifier == "*")
            {
                return "*";
            }

            if (identifier.Contains('.'))
            {
                throw new ArgumentException("Alias and column name must be passed as separate parameters. Use Column.Name(alias, columnName).", nameof(identifier));
            }

            return QuoteIdentifier(identifier);
        }

        /// <summary>
        /// Экранировать имя объекта БД, например schema.table.
        /// </summary>
        public virtual string QuoteObjectName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                throw new ArgumentException("Object name is empty", nameof(objectName));
            }

            var parts = objectName.Split('.');
            if (parts.Any(part => string.IsNullOrWhiteSpace(part)))
            {
                throw new ArgumentException($"Object name '{objectName}' contains empty identifier segment", nameof(objectName));
            }

            return string.Join(".", parts.Select(QuoteIdentifier));
        }

        /// <summary>
        /// Экранировать имя таблицы с алиасом.
        /// </summary>
        /// <param name="tableName"> Имя таблицы. </param>
        /// <param name="alias"> Алиас таблицы. </param>
        public virtual string QuoteTable(string tableName, string? alias = null)
        {
            var tableSql = QuoteObjectName(tableName);
            return string.IsNullOrWhiteSpace(alias)
                ? tableSql
                : $"{tableSql} AS {QuoteIdentifier(alias)}";
        }

        /// <summary>
        /// Получить строку литерала.
        /// В обычных запросах значения должны передаваться параметрами, а не через этот метод.
        /// </summary>
        /// <param name="param"> Значение. </param>
        public abstract string GetParameterSqlString<T>(T param);

        /// <summary>
        /// Преобразовать константу в SQL литерал конкретного диалекта.
        /// </summary>
        /// <param name="value"> Значение константы. </param>
        public virtual string FormatConstValue(object? value) => GetParameterSqlString(value);

        #endregion Методы параметров и идентификаторов

        #region Методы построения выражений

        /// <summary>
        /// Построить SQL выражение.
        /// </summary>
        /// <param name="expression"> Выражение. </param>
        /// <param name="context"> Контекст построения. </param>
        public virtual string BuildExpression(QueryExpression expression, QueryBuildContext context)
        {
            var sql = expression.ExpressionType switch
            {
                ExpressionType.SourceColumn => BuildColumnExpression(expression),
                ExpressionType.Asterisk => BuildAsteriskExpression(expression),
                ExpressionType.Parameter => context.AddParameter(expression.Value),
                ExpressionType.Const => FormatConstValue(expression.Value),
                ExpressionType.SqlText => expression.Sql ?? string.Empty,
                ExpressionType.Binary => BuildBinaryExpression(expression, context),
                ExpressionType.Unary => BuildUnaryExpression(expression, context),
                ExpressionType.Group => BuildGroupExpression(expression, context),
                ExpressionType.List => $"({string.Join(", ", expression.Children.Select(item => item.ToSql(context)))})",
                ExpressionType.Function => BuildFunctionExpression(expression, context),
                ExpressionType.SubQuery => expression.Query == null ? throw new InvalidOperationException("SubQuery is empty") : $"({expression.Query.BuildSql(context)})",
                ExpressionType.Case => BuildCaseExpression(expression, context),
                _ => throw new NotSupportedException($"Expression type {expression.ExpressionType} is not supported")
            };

            return string.IsNullOrWhiteSpace(expression.Alias)
                ? sql
                : $"{sql} AS {QuoteIdentifier(expression.Alias)}";
        }

        private string BuildColumnExpression(QueryExpression expression)
        {
            if (string.IsNullOrWhiteSpace(expression.ColumnName))
            {
                throw new InvalidOperationException("Column name is empty");
            }

            return string.IsNullOrWhiteSpace(expression.SourceAlias)
                ? QuoteQualifiedIdentifier(expression.ColumnName)
                : expression.ColumnName == "*"
                    ? $"{QuoteIdentifier(expression.SourceAlias)}.*"
                    : $"{QuoteIdentifier(expression.SourceAlias)}.{QuoteIdentifier(expression.ColumnName)}";
        }

        private string BuildAsteriskExpression(QueryExpression expression)
        {
            return string.IsNullOrWhiteSpace(expression.SourceAlias)
                ? "*"
                : $"{QuoteIdentifier(expression.SourceAlias)}.*";
        }

        private string BuildBinaryExpression(QueryExpression expression, QueryBuildContext context)
        {
            if (expression.Children.Count != 2)
            {
                throw new InvalidOperationException($"Binary expression '{expression.Operator}' must contain exactly two operands");
            }

            if (expression.ConditionOperatorType is ConditionOperator.ILike)
            {
                return BuildCaseInsensitiveLikeExpression(expression, context);
            }

            var op = expression.ConditionOperatorType.HasValue
                ? context.Engine.GetConditionOperatorSql(expression.ConditionOperatorType.Value)
                : expression.Operator;

            return $"({expression.Children[0].ToSql(context)} {op} {expression.Children[1].ToSql(context)})";
        }

        private string BuildCaseInsensitiveLikeExpression(QueryExpression expression, QueryBuildContext context)
        {
            var leftSql = $"{GetSqlFunctionSql(SqlFunction.Upper)}({expression.Children[0].ToSql(context)})";
            var rightSql = BuildCaseInsensitiveLikeValueExpression(expression.Children[1], context);
            return $"({leftSql} LIKE {rightSql})";
        }

        private string BuildCaseInsensitiveLikeValueExpression(QueryExpression expression, QueryBuildContext context)
        {
            var upperSql = GetSqlFunctionSql(SqlFunction.Upper);
            return expression.ExpressionType switch
            {
                ExpressionType.Parameter => $"{upperSql}({context.AddParameter(expression.Value)})",
                ExpressionType.Const => $"{upperSql}({FormatConstValue(expression.Value)})",
                _ => throw new NotSupportedException("Case-insensitive LIKE supports only parameter or constant right operand.")
            };
        }

        /// <summary>
        /// Получить SQL представление оператора условия.
        /// </summary>
        public virtual string GetConditionOperatorSql(ConditionOperator conditionOperator)
        {
            return conditionOperator switch
            {
                ConditionOperator.Equal => "=",
                ConditionOperator.NotEqual => "<>",
                ConditionOperator.GreaterThan => ">",
                ConditionOperator.GreaterThanOrEqual => ">=",
                ConditionOperator.LessThan => "<",
                ConditionOperator.LessThanOrEqual => "<=",
                ConditionOperator.In => "IN",
                ConditionOperator.NotIn => "NOT IN",
                ConditionOperator.Like => "LIKE",
                ConditionOperator.NotLike => "NOT LIKE",
                ConditionOperator.ILike => "ILIKE",
                ConditionOperator.IsNull => "IS NULL",
                ConditionOperator.IsNotNull => "IS NOT NULL",
                _ => throw new NotSupportedException($"Condition operator {conditionOperator} is not supported")
            };
        }

        /// <summary>
        /// Получить SQL представление логического оператора для текущего диалекта.
        /// </summary>
        /// <param name="logicalOperator">Логический оператор, который объединяет дочерние выражения группы.</param>
        /// <returns>SQL-фрагмент логического оператора.</returns>
        public virtual string GetLogicalOperatorSql(LogicalOperator logicalOperator)
        {
            return logicalOperator switch
            {
                LogicalOperator.And => "AND",
                LogicalOperator.Or => "OR",
                _ => throw new NotSupportedException($"Logical operator {logicalOperator} is not supported")
            };
        }

        /// <summary>
        /// Получить SQL представление канонической SQL-функции для текущего диалекта.
        /// </summary>
        /// <remarks>
        /// Подтипы могут переопределять для диалектов с нестандартными именами
        /// (например, <c>SUBSTRING</c> vs <c>SUBSTR</c>).
        /// </remarks>
        public virtual string GetSqlFunctionSql(SqlFunction function)
        {
            return function switch
            {
                SqlFunction.Count => "COUNT",
                SqlFunction.Sum => "SUM",
                SqlFunction.Min => "MIN",
                SqlFunction.Max => "MAX",
                SqlFunction.Avg => "AVG",
                SqlFunction.Lower => "LOWER",
                SqlFunction.Upper => "UPPER",
                SqlFunction.Coalesce => "COALESCE",
                SqlFunction.Custom => throw new InvalidOperationException(
                    "Custom SqlFunction must have non-empty Operator (raw function name)."),
                SqlFunction.None => throw new InvalidOperationException(
                    "None SqlFunction cannot be rendered. Use a concrete function or set Operator for custom."),
                _ => throw new NotSupportedException($"SQL function {function} is not supported")
            };
        }

        private string BuildFunctionExpression(QueryExpression expression, QueryBuildContext context)
        {
            // Аргументы рендерятся списком через запятую. Column.Asterisk() даёт "*" естественным образом,
            // поэтому для COUNT(*) нужно передавать Column.Asterisk() явно:
            // Func.Count(Column.Asterisk()).
            var argumentsSql = string.Join(", ", expression.Children.Select(item => item.ToSql(context)));

            var functionName = expression.SqlFunctionType == SqlFunction.Custom
                ? (expression.Operator ?? throw new InvalidOperationException(
                    "Custom function requires non-empty Operator (raw function name)."))
                : GetSqlFunctionSql(expression.SqlFunctionType);

            return $"{functionName}({argumentsSql})";
        }

        /// <summary>
        /// Получить SQL представление типа JOIN.
        /// </summary>
        public virtual string GetJoinTypeSql(JoinType joinType)
        {
            return joinType switch
            {
                JoinType.Inner => "INNER",
                JoinType.Left => "LEFT",
                JoinType.Right => "RIGHT",
                JoinType.Full => "FULL",
                JoinType.Cross => "CROSS",
                _ => throw new NotSupportedException($"Join type {joinType} is not supported")
            };
        }

        private static string BuildUnaryExpression(QueryExpression expression, QueryBuildContext context)
        {
            if (expression.Children.Count != 1)
            {
                throw new InvalidOperationException($"Unary expression '{GetUnaryOperatorDebugName(expression)}' must contain exactly one operand");
            }

            if (expression.ConditionOperatorType.HasValue)
            {
                var op = context.Engine.GetConditionOperatorSql(expression.ConditionOperatorType.Value);
                return $"({expression.Children[0].ToSql(context)} {op})";
            }

            return expression.UnaryOperatorType switch
            {
                UnaryOperator.Not => BuildNotUnaryExpression(expression.Children[0], context),
                UnaryOperator.Exists => $"EXISTS {expression.Children[0].ToSql(context)}",
                UnaryOperator.None when !string.IsNullOrWhiteSpace(expression.Operator) => $"({expression.Children[0].ToSql(context)} {expression.Operator})",
                _ => $"({expression.Children[0].ToSql(context)} {expression.Operator})"
            };
        }

        private static string GetUnaryOperatorDebugName(QueryExpression expression)
        {
            return expression.UnaryOperatorType != UnaryOperator.None
                ? expression.UnaryOperatorType.ToString()
                : expression.Operator ?? string.Empty;
        }

        private static string BuildNotUnaryExpression(QueryExpression childExpression, QueryBuildContext context)
        {
            if (childExpression.ExpressionType == ExpressionType.Unary && childExpression.ConditionOperatorType.HasValue)
            {
                var invertedOperator = childExpression.ConditionOperatorType.Value switch
                {
                    ConditionOperator.IsNull => ConditionOperator.IsNotNull,
                    ConditionOperator.IsNotNull => ConditionOperator.IsNull,
                    _ => (ConditionOperator?)null
                };

                if (invertedOperator.HasValue)
                {
                    var op = context.Engine.GetConditionOperatorSql(invertedOperator.Value);
                    return $"({childExpression.Children[0].ToSql(context)} {op})";
                }
            }

            return $"NOT ({childExpression.ToSql(context)})";
        }

        private static string BuildGroupExpression(QueryExpression expression, QueryBuildContext context)
        {
            if (expression.Children.Count == 0)
            {
                return string.Empty;
            }

            if (expression.Children.Count == 1)
            {
                return expression.Children[0].ToSql(context);
            }

            var logicalOperatorSql = expression.LogicalOperatorType.HasValue
                ? context.Engine.GetLogicalOperatorSql(expression.LogicalOperatorType.Value)
                : expression.Operator ?? throw new InvalidOperationException("Group expression requires a logical operator");

            return $"({string.Join($" {logicalOperatorSql} ", expression.Children.Select(item => item.ToSql(context)))})";
        }

        private static string BuildCaseExpression(QueryExpression expression, QueryBuildContext context)
        {
            if (expression.Children.Count < 3)
            {
                throw new InvalidOperationException("CASE expression must contain at least one WHEN/THEN and ELSE expressions");
            }

            // Поддержка как простого CASE (3 выражения), так и сложного:
            // CASE WHEN cond1 THEN val1 WHEN cond2 THEN val2 ... ELSE valN END
            if (expression.Children.Count % 2 == 0)
            {
                throw new InvalidOperationException("CASE expression must contain pairs WHEN/THEN and one ELSE expression");
            }

            var builder = new StringBuilder("CASE");
            for (var i = 0; i < expression.Children.Count - 1; i += 2)
            {
                builder.Append(" WHEN ")
                    .Append(expression.Children[i].ToSql(context))
                    .Append(" THEN ")
                    .Append(expression.Children[i + 1].ToSql(context));
            }

            builder.Append(" ELSE ")
                .Append(expression.Children[^1].ToSql(context))
                .Append(" END");

            return builder.ToString();
        }

        #endregion Методы построения выражений

        #region Методы построения запросов

        /// <summary>
        /// Построить SELECT запрос.
        /// </summary>
        public virtual string BuildSelectSql(QueryBuildContext context, SqlSelectParts parts)
        {
            var builder = new StringBuilder();
            builder.Append("SELECT");

            if (parts.Distinct)
            {
                builder.Append(" DISTINCT");
            }

            builder.AppendLine();
            builder.Append(Indent).Append(parts.Columns.Count == 0
                ? "*"
                : string.Join(", ", parts.Columns.Select(column => column.Expression.ToSql(context))));

            if (!string.IsNullOrWhiteSpace(parts.From))
            {
                builder.AppendLine().Append("FROM").AppendLine();
                builder.Append(Indent).Append(QuoteTable(parts.From, parts.FromAlias));
            }

            foreach (var join in parts.Joins)
            {
                builder.AppendLine().Append(GetJoinTypeSql(join.JoinType)).Append(" JOIN").AppendLine();
                builder.Append(Indent).Append(QuoteTable(join.TableName, join.Alias));
                if (join.On != null)
                {
                    builder.AppendLine().Append("ON").AppendLine();
                    builder.Append(Indent).Append(join.On.ToSql(context));
                }
            }

            if (parts.Where != null)
            {
                builder.AppendLine().Append("WHERE").AppendLine();
                builder.Append(Indent).Append(parts.Where.ToSql(context));
            }

            if (parts.GroupBy.Count > 0)
            {
                builder.AppendLine().Append("GROUP BY").AppendLine();
                builder.Append(Indent).Append(string.Join(", ", parts.GroupBy.Select(item => item.ToSql(context))));
            }

            if (parts.Having != null)
            {
                builder.AppendLine().Append("HAVING").AppendLine();
                builder.Append(Indent).Append(parts.Having.ToSql(context));
            }

            if (parts.OrderBy.Count > 0)
            {
                builder.AppendLine().Append("ORDER BY").AppendLine();
                builder.Append(Indent).Append(string.Join(", ", parts.OrderBy.Select(order => $"{order.Expression.ToSql(context)} {(order.Desc ? "DESC" : "ASC")}")));
            }

            if (parts.Limit.HasValue)
            {
                builder.AppendLine().Append("LIMIT").AppendLine();
                builder.Append(Indent).Append(context.AddParameter(parts.Limit.Value));
            }

            if (parts.Offset.HasValue)
            {
                builder.AppendLine().Append("OFFSET").AppendLine();
                builder.Append(Indent).Append(context.AddParameter(parts.Offset.Value));
            }
            
            var sql = builder.ToString();
            foreach (var union in parts.Unions)
            {
                sql = $"({sql}){Environment.NewLine}UNION{(union.All ? " ALL" : string.Empty)}{Environment.NewLine}({union.Query.BuildSql(context)})";
            }

            return sql;
        }

        /// <summary>
        /// Построить UPDATE запрос.
        /// </summary>
        public virtual string BuildUpdateSql(QueryBuildContext context, SqlUpdateParts parts)
        {
            var builder = new StringBuilder();
            builder.Append("UPDATE").AppendLine();
            builder.Append(Indent).Append(QuoteTable(parts.TableName, parts.Alias));
            builder.AppendLine().Append("SET").AppendLine();
            builder.Append(Indent).Append(string.Join($",{Environment.NewLine}{Indent}", parts.Set.Select(item => $"{item.Column.ToSql(context)} = {item.Value.ToSql(context)}")));

            if (parts.From.Count > 0)
            {
                builder.AppendLine().Append("FROM").AppendLine();
                builder.Append(Indent).Append(string.Join($",{Environment.NewLine}{Indent}", parts.From.Select(table => QuoteTable(table.TableName, table.Alias))));
            }

            if (parts.Where != null)
            {
                builder.AppendLine().Append("WHERE").AppendLine();
                builder.Append(Indent).Append(parts.Where.ToSql(context));
            }

            AppendReturning(builder, context, parts.Returning);
            return builder.ToString();
        }

        /// <summary>
        /// Построить DELETE запрос.
        /// </summary>
        public virtual string BuildDeleteSql(QueryBuildContext context, SqlDeleteParts parts)
        {
            var builder = new StringBuilder();
            builder.Append("DELETE FROM").AppendLine();
            builder.Append(Indent).Append(QuoteTable(parts.TableName, parts.Alias));

            if (parts.Using.Count > 0)
            {
                builder.AppendLine().Append("USING").AppendLine();
                builder.Append(Indent).Append(string.Join($",{Environment.NewLine}{Indent}", parts.Using.Select(table => QuoteTable(table.TableName, table.Alias))));
            }

            if (parts.Where != null)
            {
                builder.AppendLine().Append("WHERE").AppendLine();
                builder.Append(Indent).Append(parts.Where.ToSql(context));
            }

            AppendReturning(builder, context, parts.Returning);
            return builder.ToString();
        }

        /// <summary>
        /// Построить INSERT запрос.
        /// </summary>
        public virtual string BuildInsertSql(QueryBuildContext context, SqlInsertParts parts)
        {
            var builder = new StringBuilder();
            builder.Append("INSERT INTO").AppendLine();
            builder.Append(Indent).Append(QuoteObjectName(parts.TableName));

            if (parts.Columns.Count > 0)
            {
                builder.AppendLine().Append('(').AppendLine();
                builder.Append(Indent).Append(string.Join($",{Environment.NewLine}{Indent}", parts.Columns.Select(QuoteQualifiedIdentifier))).AppendLine();
                builder.Append(')');
            }

            if (parts.Select != null)
            {
                builder.AppendLine().Append(parts.Select.BuildSql(context));
            }
            else
            {
                builder.AppendLine().Append("VALUES").AppendLine();
                builder.Append(Indent).Append(string.Join($",{Environment.NewLine}{Indent}", parts.Rows.Select(row => $"({string.Join(", ", row.Select(expression => expression.ToSql(context)))})")));
            }

            if (parts.OnConflictDoNothing)
            {
                builder.AppendLine().Append(parts.ConflictColumns.Count == 0
                    ? "ON CONFLICT DO NOTHING"
                    : $"ON CONFLICT ({string.Join(", ", parts.ConflictColumns.Select(QuoteQualifiedIdentifier))}) DO NOTHING");
            }
            else if (parts.ConflictUpdateSet.Count > 0)
            {
                if (parts.ConflictColumns.Count == 0)
                {
                    throw new InvalidOperationException("ON CONFLICT DO UPDATE requires conflict columns");
                }

                builder.AppendLine().Append($"ON CONFLICT ({string.Join(", ", parts.ConflictColumns.Select(QuoteQualifiedIdentifier))}) DO UPDATE");
                builder.AppendLine().Append("SET").AppendLine();
                builder.Append(Indent).Append(string.Join($",{Environment.NewLine}{Indent}", parts.ConflictUpdateSet.Select(item => $"{item.Column.ToSql(context)} = {item.Value.ToSql(context)}")));
            }
            else if (!string.IsNullOrWhiteSpace(parts.ConflictSql))
            {
                builder.AppendLine().Append(parts.ConflictSql);
            }

            AppendReturning(builder, context, parts.Returning);
            return builder.ToString();
        }

        private static void AppendReturning(StringBuilder builder, QueryBuildContext context, IReadOnlyList<QueryExpression> returning)
        {
            if (returning.Count > 0)
            {
                builder.AppendLine().Append("RETURNING").AppendLine();
                builder.Append(context.Engine.Indent).Append(string.Join(", ", returning.Select(expression => expression.ToSql(context))));
            }
        }

        #endregion Методы построения запросов
    }
}
