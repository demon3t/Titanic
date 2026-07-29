using System.Text.Json;
using System.Text.Json.Serialization;

namespace Titanic.Entity.Orm
{
    /// <summary>
    /// JSON-модель фильтра ESQ.
    /// </summary>
    public sealed class ESQFilterJsonModel
    {
        /// <summary>
        /// ORM-путь колонки фильтра.
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Тип сравнения фильтра.
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public EntityComparisonType ComparisonType { get; set; }

        /// <summary>
        /// Первое значение фильтра.
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// Второе значение фильтра для диапазона.
        /// </summary>
        public object? SecondValue { get; set; }

        /// <summary>
        /// Признак активности фильтра или группы.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Признак отрицания фильтра.
        /// </summary>
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
                filter.WithRange(
                    EntityValueNormalizer.NormalizeJsonValue(Value),
                    EntityValueNormalizer.NormalizeJsonValue(SecondValue));
            }
            else
            {
                filter.WithValue(EntityValueNormalizer.NormalizeJsonValue(Value));
            }

            if (IsNot)
            {
                filter.Not();
            }

            return filter;
        }

    }
}
