using System.Data.Common;
using Titanic.Common.Session;
using Titanic.Db;
using Titanic.Db.Abstractions;
using Titanic.Entity.Interfaces;
using Titanic.Entity.Strurture;

namespace Titanic.Entity.Orm
{
    /// <summary>
    /// Entity Schema Query: модель запроса для чтения сущностей из базы данных.
    /// </summary>
    public class EntitySchemaQuery
    {
        private readonly BaseDbProvider _provider;
        private readonly EntityStructure _structure;
        private readonly EntityStructureScope _structureScope;
        private readonly BaseEntityManager? _manager;
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
            : this(provider, Structure.DefaultScope, entityType, userConnection)
        {
        }

        public EntitySchemaQuery(BaseDbProvider provider, string tableName, UserConnection userConnection)
            : this(provider, Structure.DefaultScope, tableName, userConnection)
        {
        }

        internal EntitySchemaQuery(
            BaseDbProvider provider,
            EntityStructureScope structureScope,
            Type entityType,
            UserConnection userConnection,
            BaseEntityManager? manager = null)
            : this(provider, structureScope.GetEntityStructure(entityType), structureScope, userConnection, manager)
        {
        }

        internal EntitySchemaQuery(
            BaseDbProvider provider,
            EntityStructureScope structureScope,
            string tableName,
            UserConnection userConnection,
            BaseEntityManager? manager = null)
            : this(provider, structureScope.GetEntityStructure(tableName), structureScope, userConnection, manager)
        {
        }

        internal EntitySchemaQuery(BaseDbProvider provider, EntityStructure structure, UserConnection userConnection)
            : this(provider, structure, Structure.DefaultScope, userConnection)
        {
        }

        internal EntitySchemaQuery(
            BaseDbProvider provider,
            EntityStructure structure,
            EntityStructureScope structureScope,
            UserConnection userConnection,
            BaseEntityManager? manager = null)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _structure = structure ?? throw new ArgumentNullException(nameof(structure));
            _structureScope = structureScope ?? throw new ArgumentNullException(nameof(structureScope));
            _manager = manager;
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

        public EntityQueryFilter CreateFilter(EntityComparisonType comparisonType, string columnPath, object? value)
            => new EntityQueryFilter(columnPath, comparisonType).WithValue(value);

        /// <summary>
        /// Создать фильтр поиска по вхождению.
        /// </summary>
        public EntityQueryFilter CreateContainsFilter(string columnPath, object? value)
            => CreateFilter(EntityComparisonType.Contains, columnPath, value);

        /// <summary>
        /// Создать фильтр поиска по началу строки.
        /// </summary>
        public EntityQueryFilter CreateStartsWithFilter(string columnPath, object? value)
            => CreateFilter(EntityComparisonType.StartsWith, columnPath, value);

        /// <summary>
        /// Создать фильтр поиска по концу строки.
        /// </summary>
        public EntityQueryFilter CreateEndsWithFilter(string columnPath, object? value)
            => CreateFilter(EntityComparisonType.EndsWith, columnPath, value);

        public EntityQueryFilter CreateIsNullFilter(string columnPath)
            => new EntityQueryFilter(columnPath, EntityComparisonType.IsNull);

        public EntityQueryFilter CreateIsNotNullFilter(string columnPath)
            => new EntityQueryFilter(columnPath, EntityComparisonType.IsNotNull);

        public EntityQueryFilter CreateBetweenFilter(string columnPath, object? from, object? to)
            => new EntityQueryFilter(columnPath, EntityComparisonType.Equal).WithRange(from, to);

        public EntityQueryFilter AddFilter(EntityComparisonType comparisonType, string columnPath, object? value = null)
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
            var builder = new EntitySelectBuilder(_provider, _structure, _structureScope, UserConnection, _manager);

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
            var expression = filter.SecondValue != null
                ? EntityComparisonExpressionBuilder.BuildBetween(left, filter.Value, filter.SecondValue)
                : EntityComparisonExpressionBuilder.Build(left, filter.ComparisonType, filter.Value);

            return filter.IsNot ? QueryExpression.Not(expression) : expression;
        }

        internal sealed record EntityQueryOrder(string Path, bool Desc);
    }
}





