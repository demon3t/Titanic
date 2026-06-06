using System.Text.Json;
using System.Text.Json.Serialization;
using Titanic.Common.Session;
using Titanic.Db.Abstractions;
using Titanic.Db.Enums;
using Titanic.Entity.Strurture;

namespace Titanic.Entity.Orm
{
    /// <summary>
    /// JSON-friendly ESQ model for HTTP requests.
    /// </summary>
    public sealed class ESQJsonModel
    {
        public string? TableName { get; set; }

        public string? EntityTypeName { get; set; }

        public List<ESQColumnJsonModel> Columns { get; set; } = [];

        public ESQFilterCollectionJsonModel Filters { get; set; } = new();

        /// <summary>
        /// ORM-пути колонок группировки.
        /// </summary>
        public List<string> GroupBy { get; set; } = [];

        public List<ESQOrderJsonModel> Orders { get; set; } = [];

        public bool IsDistinct { get; set; }

        /// <summary>
        /// Признак выбора всех колонок корневой схемы без перечисления в Columns.
        /// </summary>
        public bool AllColumns { get; set; }

        public int? SkipRowCount { get; set; }

        public int? SkipRow { get; set; }

        public int? RowCount { get; set; }

        public static ESQJsonModel FromJson(string json, JsonSerializerOptions? options = null)
        {
            return JsonSerializer.Deserialize<ESQJsonModel>(json, options ?? DefaultJsonOptions())
                ?? throw new InvalidOperationException("ESQ JSON model is empty.");
        }

        public string ToJson(JsonSerializerOptions? options = null)
        {
            return JsonSerializer.Serialize(this, options ?? DefaultJsonOptions());
        }

        public EntitySchemaQuery ToESQ(BaseDbProvider provider, UserConnection userConnection)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(userConnection);

            var esq = CreateESQ(provider, userConnection);
            esq.IsDistinct = IsDistinct;
            esq.AllColumns = AllColumns || Columns.Count == 0 || Columns.Any(IsAllColumnsMarker);
            esq.SkipRow = SkipRow ?? SkipRowCount;
            esq.RowCount = RowCount;

            foreach (var column in Columns.Where(column => !IsAllColumnsMarker(column)))
            {
                if (column.AggregationType == EntityAggregationType.None)
                {
                    esq.AddColumn(column.Path, column.Alias);
                }
                else
                {
                    esq.AddAggregationColumn(column.Path, column.AggregationType, column.Alias);
                }
            }

            CopyFilterCollection(Filters.ToEntityFilterCollection(), esq.Filters);

            foreach (var groupBy in GroupBy)
            {
                esq.GroupBy(groupBy);
            }

            foreach (var order in Orders)
            {
                esq.OrderBy(order.Path, order.Desc);
            }

            return esq;
        }

        public EntitySchemaQuery ToESQ(BaseDatabase database, UserConnection userConnection)
        {
            ArgumentNullException.ThrowIfNull(database);
            ArgumentNullException.ThrowIfNull(userConnection);
            return ToESQ(database.Select().Provider
                ?? throw new InvalidOperationException("Database provider is not initialized."),
                userConnection);
        }

        internal static ESQJsonModel FromESQ(EntitySchemaQuery esq, string tableName)
        {
            ArgumentNullException.ThrowIfNull(esq);

            return new ESQJsonModel
            {
                TableName = tableName,
                IsDistinct = esq.IsDistinct,
                AllColumns = esq.AllColumns,
                SkipRowCount = esq.SkipRowCount,
                SkipRow = esq.SkipRow,
                RowCount = esq.RowCount,
                GroupBy = esq.GroupByColumns.ToList(),
                Columns = esq.Columns.Select(x => new ESQColumnJsonModel
                {
                    Path = x.Path,
                    Alias = x.Alias,
                    AggregationType = x.AggregationType
                }).ToList(),
                Filters = ESQFilterCollectionJsonModel.FromEntityCollection(esq.Filters),
                Orders = esq.Orders.Select(x => new ESQOrderJsonModel
                {
                    Path = x.Path,
                    Desc = x.Desc
                }).ToList()
            };
        }

        private EntitySchemaQuery CreateESQ(BaseDbProvider provider, UserConnection userConnection)
        {
            if (!string.IsNullOrWhiteSpace(TableName))
            {
                return new EntitySchemaQuery(provider, TableName, userConnection);
            }

            if (!string.IsNullOrWhiteSpace(EntityTypeName))
            {
                return new EntitySchemaQuery(provider, Structure.GetEntityStructureByTypeName(EntityTypeName), userConnection);
            }

            throw new InvalidOperationException("ESQ JSON model must contain TableName or EntityTypeName.");
        }

        private static bool IsAllColumnsMarker(ESQColumnJsonModel column)
        {
            return column.AggregationType == EntityAggregationType.None
                && (string.Equals(column.Path, "*", StringComparison.OrdinalIgnoreCase)
                || string.Equals(column.Path, "All", StringComparison.OrdinalIgnoreCase)
                || string.Equals(column.Path, "AllColumns", StringComparison.OrdinalIgnoreCase));
        }

        private static JsonSerializerOptions DefaultJsonOptions()
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            options.Converters.Add(new JsonStringEnumConverter());
            return options;
        }

        private static void CopyFilterCollection(
            EntityQueryFilterCollection source,
            EntityQueryFilterCollection target)
        {
            target.IsEnabled = source.IsEnabled;
            target.LogicalOperation = source.LogicalOperation;

            foreach (var node in source.Nodes)
            {
                switch (node)
                {
                    case EntityQueryFilter filter:
                        target.Add(filter);
                        break;
                    case EntityQueryFilterCollection group:
                        target.Add(group);
                        break;
                }
            }
        }
    }
}

