using System.Data.Common;
using Titanic.Common.Session;
using Titanic.Db;
using Titanic.Db.Abstractions;
using Titanic.Db.Enums;
using Titanic.Entity.Strurture;

namespace Titanic.Entity.Orm
{
    /// <summary>
    /// Entity Schema Query: query model for reading entities from the database.
    /// </summary>
    public class EntitySchemaQuery
    {
        private readonly BaseDbProvider _provider;
        private readonly EntityStructure _structure;
        private readonly List<EntityQueryOrder> _orders = new();
        private readonly List<string> _groupBy = new();

        public EntityQueryColumnCollection Columns { get; } = new();

        public EntityQueryFilterCollection Filters { get; } = new();

        internal IReadOnlyList<EntityQueryOrder> Orders => _orders;

        /// <summary>
        /// Колонки группировки запроса.
        /// </summary>
        public IReadOnlyList<string> GroupByColumns => _groupBy;

        public bool IsDistinct { get; set; }

        /// <summary>
        /// Признак выбора всех колонок корневой схемы.
        /// </summary>
        public bool AllColumns { get; set; }

        public int? SkipRowCount { get; set; }

        public int? SkipRow
        {
            get => SkipRowCount;
            set => SkipRowCount = value;
        }

        public int? RowCount { get; set; }

        /// <summary>
        /// Максимальное количество строк, которое разрешено считать этим запросом.
        /// </summary>
        public int? MaxReadRowCount { get; set; }

        public UserConnection UserConnection { get; }

        public EntitySchemaQuery(BaseDbProvider provider, Type entityType, UserConnection userConnection)
            : this(provider, Structure.GetEntityStructure(entityType), userConnection)
        {
        }

        public EntitySchemaQuery(BaseDbProvider provider, string tableName, UserConnection userConnection)
            : this(provider, Structure.GetEntityStructure(tableName), userConnection)
        {
        }

        internal EntitySchemaQuery(BaseDbProvider provider, EntityStructure structure, UserConnection userConnection)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _structure = structure ?? throw new ArgumentNullException(nameof(structure));
            UserConnection = userConnection ?? throw new ArgumentNullException(nameof(userConnection));
        }

        public EntityQueryColumn AddColumn(string path, string? alias = null) => Columns.Add(path, alias);

        /// <summary>
        /// Добавить агрегатную колонку.
        /// </summary>
        /// <param name="path"> ORM-путь колонки или <c>*</c> для COUNT(*). </param>
        /// <param name="aggregationType"> Тип агрегатной функции. </param>
        /// <param name="alias"> Алиас результата. </param>
        /// <returns> Описание добавленной колонки. </returns>
        public EntityQueryColumn AddAggregationColumn(
            string path,
            EntityAggregationType aggregationType,
            string? alias = null)
            => Columns.Add(path, alias, aggregationType);

        public EntityQueryColumn AddPrimaryColumn(string? alias = null)
            => AddColumn(_structure.GetPrimaryColumnStructure().PropertyName, alias);

        public EntityQueryColumn AddDisplayColumn(string? alias = null)
            => AddColumn(_structure.GetDisplayColumnStructure().PropertyName, alias);

        public EntitySchemaQuery AddAllSchemaColumns()
        {
            foreach (var column in _structure.ColumnsStructure)
            {
                AddColumn(column.PropertyName);
            }

            return this;
        }

        public EntityQueryFilter CreateFilter(ConditionOperator comparisonType, string columnPath, object? value)
            => new EntityQueryFilter(columnPath, comparisonType).WithValue(value);

        /// <summary>
        /// Создать фильтр поиска по вхождению.
        /// </summary>
        public EntityQueryFilter CreateContainsFilter(string columnPath, object? value)
            => CreateFilter(ConditionOperator.Contains, columnPath, value);

        /// <summary>
        /// Создать фильтр поиска по началу строки.
        /// </summary>
        public EntityQueryFilter CreateStartsWithFilter(string columnPath, object? value)
            => CreateFilter(ConditionOperator.StartsWith, columnPath, value);

        /// <summary>
        /// Создать фильтр поиска по концу строки.
        /// </summary>
        public EntityQueryFilter CreateEndsWithFilter(string columnPath, object? value)
            => CreateFilter(ConditionOperator.EndsWith, columnPath, value);

        public EntityQueryFilter CreateIsNullFilter(string columnPath)
            => new EntityQueryFilter(columnPath, ConditionOperator.IsNull);

        public EntityQueryFilter CreateIsNotNullFilter(string columnPath)
            => new EntityQueryFilter(columnPath, ConditionOperator.IsNotNull);

        public EntityQueryFilter CreateBetweenFilter(string columnPath, object? from, object? to)
            => new EntityQueryFilter(columnPath, ConditionOperator.Equal).WithRange(from, to);

        public EntityQueryFilter AddFilter(ConditionOperator comparisonType, string columnPath, object? value = null)
            => Filters.Add(columnPath, comparisonType, value);

        /// <summary>
        /// Добавить фильтр поиска по вхождению.
        /// </summary>
        public EntityQueryFilter AddContainsFilter(string columnPath, object? value)
            => Filters.AddContains(columnPath, value);

        /// <summary>
        /// Добавить фильтр поиска по началу строки.
        /// </summary>
        public EntityQueryFilter AddStartsWithFilter(string columnPath, object? value)
            => Filters.AddStartsWith(columnPath, value);

        /// <summary>
        /// Добавить фильтр поиска по концу строки.
        /// </summary>
        public EntityQueryFilter AddEndsWithFilter(string columnPath, object? value)
            => Filters.AddEndsWith(columnPath, value);

        public EntityQueryFilter AddBetweenFilter(string columnPath, object? from, object? to)
            => Filters.AddBetween(columnPath, from, to);

        public EntityQueryFilter AddIsNullFilter(string columnPath)
            => Filters.AddIsNull(columnPath);

        public EntityQueryFilter AddIsNotNullFilter(string columnPath)
            => Filters.AddIsNotNull(columnPath);

        public EntityQueryFilter AddFilter(EntityQueryFilter filter)
            => Filters.Add(filter);

        public EntitySchemaQuery OrderBy(string path, bool desc = false)
        {
            _orders.Add(new EntityQueryOrder(path, desc));
            return this;
        }

        public EntitySchemaQuery ThenBy(string path, bool desc = false) => OrderBy(path, desc);

        /// <summary>
        /// Добавить группировку по ORM-пути.
        /// </summary>
        /// <param name="path"> ORM-путь колонки. </param>
        /// <returns> Текущий запрос. </returns>
        public EntitySchemaQuery GroupBy(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("GroupBy path is empty", nameof(path));
            }

            _groupBy.Add(path);
            return this;
        }

        public EntitySelectBuilder BuildSelect()
        {
            var builder = new EntitySelectBuilder(_provider, _structure, UserConnection);

            if (IsDistinct)
            {
                builder.Distinct();
            }

            if (AllColumns || Columns.Count == 0)
            {
                foreach (var column in _structure.ColumnsStructure)
                {
                    builder.AddColumn(column.PropertyName);
                }
            }

            foreach (var column in Columns.Items)
            {
                if (column.AggregationType == EntityAggregationType.None)
                {
                    builder.AddColumn(column.Path, column.Alias);
                }
                else
                {
                    builder.AddAggregateColumn(column.Path, column.AggregationType, column.Alias);
                }
            }

            ApplyFilters(builder);

            foreach (var groupBy in _groupBy)
            {
                builder.GroupBy(groupBy);
            }

            foreach (var order in _orders)
            {
                builder.OrderBy(order.Path, order.Desc);
            }

            var rowCount = GetEffectiveRowCount();

            if (rowCount.HasValue && SkipRowCount.HasValue)
            {
                builder.Select.Limit(rowCount.Value).Skip(SkipRowCount.Value);
            }
            else if (rowCount.HasValue)
            {
                builder.Take(rowCount.Value);
            }
            else if (SkipRowCount.HasValue)
            {
                builder.Select.Limit(int.MaxValue).Skip(SkipRowCount.Value);
            }

            return builder;
        }

        /// <summary>
        /// Получить итоговый LIMIT с учётом ограничения менеджера.
        /// </summary>
        /// <returns> Итоговое количество строк для чтения. </returns>
        private int? GetEffectiveRowCount()
        {
            if (!MaxReadRowCount.HasValue || MaxReadRowCount.Value <= 0)
            {
                return RowCount;
            }

            if (!RowCount.HasValue)
            {
                return MaxReadRowCount;
            }

            return Math.Min(RowCount.Value, MaxReadRowCount.Value);
        }

        public QueryBuildResult Build() => BuildSelect().Build();

        public string GetSqlText() => BuildSelect().ToSql();

        public List<Entity> GetEntityCollection() => BuildSelect().ToRecords();

        public List<T> ExecuteReader<T>(Func<DbDataReader, T> mapRow) => BuildSelect().ExecuteReader(mapRow);

        public ESQJsonModel ToJsonModel()
        {
            return ESQJsonModel.FromESQ(this, _structure.TableName);
        }

        private void ApplyFilters(EntitySelectBuilder builder)
        {
            var expression = BuildFilterCollectionExpression(builder, Filters);
            if (expression != null)
            {
                builder.AddWhereExpression(expression, EntityWhereConnector.And);
            }
        }

        private static QueryExpression? BuildFilterCollectionExpression(
            EntitySelectBuilder builder,
            EntityQueryFilterCollection collection)
        {
            if (!collection.IsEnabled)
            {
                return null;
            }

            QueryExpression? expression = null;
            foreach (var node in collection.Nodes.Where(x => x.IsEnabled))
            {
                var child = node switch
                {
                    EntityQueryFilter filter => BuildFilterExpression(builder, filter),
                    EntityQueryFilterCollection group => BuildFilterCollectionExpression(builder, group),
                    _ => null
                };

                if (child == null)
                {
                    continue;
                }

                expression = expression == null
                    ? child
                    : collection.LogicalOperation == EntityLogicalOperation.Or
                        ? QueryExpression.Or(expression, child)
                        : QueryExpression.And(expression, child);
            }

            return expression;
        }

        private static QueryExpression BuildFilterExpression(EntitySelectBuilder builder, EntityQueryFilter filter)
        {
            var left = builder.BuildWhereColumnExpression(filter.Path);
            QueryExpression expression;

            if (filter.SecondValue != null)
            {
                expression = QueryExpression.And(
                    BuildBinary(left, ConditionOperator.GreaterThanOrEqual, Column.Parameter(filter.Value)),
                    BuildBinary(left, ConditionOperator.LessThanOrEqual, Column.Parameter(filter.SecondValue)));
                return filter.IsNot ? QueryExpression.Not(expression) : expression;
            }

            expression = filter.ComparisonType switch
            {
                ConditionOperator.Equal when filter.Value == null => BuildUnary(left, ConditionOperator.IsNull),
                ConditionOperator.Equal => BuildBinary(left, ConditionOperator.Equal, Column.Parameter(filter.Value)),
                ConditionOperator.NotEqual when filter.Value == null => BuildUnary(left, ConditionOperator.IsNotNull),
                ConditionOperator.NotEqual => BuildBinary(left, ConditionOperator.NotEqual, Column.Parameter(filter.Value)),
                ConditionOperator.GreaterThan => BuildBinary(left, ConditionOperator.GreaterThan, Column.Parameter(filter.Value)),
                ConditionOperator.GreaterThanOrEqual => BuildBinary(left, ConditionOperator.GreaterThanOrEqual, Column.Parameter(filter.Value)),
                ConditionOperator.LessThan => BuildBinary(left, ConditionOperator.LessThan, Column.Parameter(filter.Value)),
                ConditionOperator.LessThanOrEqual => BuildBinary(left, ConditionOperator.LessThanOrEqual, Column.Parameter(filter.Value)),
                ConditionOperator.Contains => BuildBinary(left, ConditionOperator.Contains, Column.Parameter(filter.Value)),
                ConditionOperator.StartsWith => BuildBinary(left, ConditionOperator.StartsWith, Column.Parameter(filter.Value)),
                ConditionOperator.EndsWith => BuildBinary(left, ConditionOperator.EndsWith, Column.Parameter(filter.Value)),
                ConditionOperator.IsNull => BuildUnary(left, ConditionOperator.IsNull),
                ConditionOperator.IsNotNull => BuildUnary(left, ConditionOperator.IsNotNull),
                ConditionOperator.In or ConditionOperator.NotIn => BuildInExpression(left, filter),
                _ => throw new NotSupportedException(
                    $"Filter operator '{filter.ComparisonType}' is not supported by EntitySchemaQuery.")
            };

            return filter.IsNot ? QueryExpression.Not(expression) : expression;
        }

        private static QueryExpression BuildBinary(QueryExpression left, ConditionOperator op, QueryExpression value)
        {
            return QueryExpression.Binary(left, op, value);
        }

        private static QueryExpression BuildUnary(QueryExpression left, ConditionOperator op)
        {
            return EntityQueryExpression.Unary(op, left);
        }

        private static QueryExpression BuildInExpression(QueryExpression left, EntityQueryFilter filter)
        {
            if (filter.Value is not BaseQuery subQuery)
            {
                throw new NotSupportedException(
                    "EntitySchemaQuery currently supports IN/NOT IN only with BaseQuery values.");
            }

            var op = filter.ComparisonType == ConditionOperator.NotIn
                ? ConditionOperator.NotIn
                : ConditionOperator.In;
            return BuildBinary(left, op, QueryExpression.SubQuery(subQuery));
        }

        internal sealed record EntityQueryOrder(string Path, bool Desc);
    }
}

