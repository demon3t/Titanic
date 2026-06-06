using System.Collections;

namespace Titanic.Entity.Orm
{
    /// <summary>
    /// Коллекция колонок ORM-запроса.
    /// </summary>
    public sealed class EntityQueryColumnCollection : IReadOnlyCollection<EntityQueryColumn>
    {
        private readonly List<EntityQueryColumn> _items = [];

        /// <summary>
        /// Количество колонок.
        /// </summary>
        public int Count => _items.Count;

        /// <summary>
        /// Добавить колонку по ORM-пути.
        /// </summary>
        public EntityQueryColumn Add(
            string path,
            string? alias = null,
            EntityAggregationType aggregationType = EntityAggregationType.None)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Column path is empty", nameof(path));
            }

            var column = new EntityQueryColumn(path, alias, aggregationType);
            _items.Add(column);
            return column;
        }

        internal IReadOnlyList<EntityQueryColumn> Items => _items;

        public IEnumerator<EntityQueryColumn> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
