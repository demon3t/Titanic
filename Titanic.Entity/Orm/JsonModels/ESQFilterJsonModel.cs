using System.Text.Json;
using System.Text.Json.Serialization;
using Titanic.Db.Enums;

namespace Titanic.Entity.Orm
{
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
}
