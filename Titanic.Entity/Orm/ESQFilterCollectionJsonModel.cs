using System.Text.Json.Serialization;

namespace Titanic.Entity.Orm
{
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
}
