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

    public sealed class ESQColumnJsonModel
    {
        public string Path { get; set; } = string.Empty;

        public string? Alias { get; set; }

        /// <summary>
        /// Тип агрегатной функции для колонки.
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public EntityAggregationType AggregationType { get; set; }
    }

    public sealed class ESQFilterCollectionJsonModel
    {
        public bool IsEnabled { get; set; } = true;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public EntityLogicalOperation LogicalOperation { get; set; } = EntityLogicalOperation.And;

        public List<ESQFilterJsonModel> Items { get; set; } = [];

        internal EntityQueryFilterCollection ToEntityFilterCollection()
        {
            var collection = new EntityQueryFilterCollection
            {
                IsEnabled = IsEnabled,
                LogicalOperation = LogicalOperation
            };

            foreach (var item in Items)
            {
                AddNode(collection, item.ToEntityNode());
            }

            return collection;
        }

        internal static ESQFilterCollectionJsonModel FromEntityCollection(EntityQueryFilterCollection collection)
        {
            return new ESQFilterCollectionJsonModel
            {
                IsEnabled = collection.IsEnabled,
                LogicalOperation = collection.LogicalOperation,
                Items = collection.Nodes.Select(ESQFilterJsonModel.FromEntityNode).ToList()
            };
        }

        private static void AddNode(EntityQueryFilterCollection collection, EntityQueryFilterNode node)
        {
            switch (node)
            {
                case EntityQueryFilter filter:
                    collection.Add(filter);
                    break;
                case EntityQueryFilterCollection group:
                    collection.Add(group);
                    break;
            }
        }
    }

    public sealed class ESQFilterJsonModel
    {
        public string Path { get; set; } = string.Empty;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ConditionOperator ComparisonType { get; set; }

        public object? Value { get; set; }

        public object? SecondValue { get; set; }

        public bool IsEnabled { get; set; } = true;

        public bool IsNot { get; set; }

        /// <summary>
        /// Логическая операция между вложенными фильтрами, если элемент является группой.
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public EntityLogicalOperation LogicalOperation { get; set; } = EntityLogicalOperation.And;

        /// <summary>
        /// Вложенные элементы фильтра. Если коллекция заполнена, текущий элемент считается группой.
        /// </summary>
        public List<ESQFilterJsonModel> Items { get; set; } = [];

        internal static ESQFilterJsonModel FromEntityFilter(EntityQueryFilter filter)
        {
            return new ESQFilterJsonModel
            {
                Path = filter.Path,
                ComparisonType = filter.ComparisonType,
                Value = filter.Value,
                SecondValue = filter.SecondValue,
                IsEnabled = filter.IsEnabled,
                IsNot = filter.IsNot
            };
        }

        internal static ESQFilterJsonModel FromEntityNode(EntityQueryFilterNode node)
        {
            return node switch
            {
                EntityQueryFilter filter => FromEntityFilter(filter),
                EntityQueryFilterCollection group => new ESQFilterJsonModel
                {
                    IsEnabled = group.IsEnabled,
                    LogicalOperation = group.LogicalOperation,
                    Items = group.Nodes.Select(FromEntityNode).ToList()
                },
                _ => throw new NotSupportedException($"Filter node '{node.GetType().Name}' is not supported.")
            };
        }

        internal EntityQueryFilterNode ToEntityNode()
        {
            if (Items.Count > 0 || string.IsNullOrWhiteSpace(Path))
            {
                var group = new EntityQueryFilterCollection
                {
                    IsEnabled = IsEnabled,
                    LogicalOperation = LogicalOperation
                };

                foreach (var item in Items)
                {
                    switch (item.ToEntityNode())
                    {
                        case EntityQueryFilter filter:
                            group.Add(filter);
                            break;
                        case EntityQueryFilterCollection nestedGroup:
                            group.Add(nestedGroup);
                            break;
                    }
                }

                return group;
            }

            return ToEntityFilter();
        }

        private EntityQueryFilter ToEntityFilter()
        {
            var filter = new EntityQueryFilter(Path, ComparisonType)
            {
                IsEnabled = IsEnabled
            };

            if (SecondValue != null)
            {
                filter.WithRange(NormalizeJsonValue(Value), NormalizeJsonValue(SecondValue));
            }
            else
            {
                filter.WithValue(NormalizeJsonValue(Value));
            }

            if (IsNot)
            {
                filter.Not();
            }

            return filter;
        }

        private static object? NormalizeJsonValue(object? value)
        {
            if (value is JsonElement element)
            {
                return NormalizeJsonElement(element);
            }

            return value;
        }

        private static object? NormalizeJsonElement(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Null => null,
                JsonValueKind.Undefined => null,
                JsonValueKind.String => element.GetString(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number when element.TryGetInt32(out var intValue) => intValue,
                JsonValueKind.Number when element.TryGetInt64(out var longValue) => longValue,
                JsonValueKind.Number when element.TryGetDecimal(out var decimalValue) => decimalValue,
                JsonValueKind.Number => element.GetDouble(),
                _ => throw new NotSupportedException($"JSON value kind '{element.ValueKind}' is not supported in ESQ filters.")
            };
        }
    }

    public sealed class ESQOrderJsonModel
    {
        public string Path { get; set; } = string.Empty;

        public bool Desc { get; set; }
    }
}

