using System.Collections;

namespace Titanic.Entity.Orm
{
    /// <summary>
    /// Коллекция фильтров EntitySchemaQuery. Может содержать leaf-фильтры и вложенные группы.
    /// </summary>
    public sealed class EntityQueryFilterCollection : EntityQueryFilterNode, IReadOnlyCollection<EntityQueryFilter>
    {
        #region Fields

        private readonly List<EntityQueryFilterNode> _nodes = [];

        #endregion Fields

        #region Properties

        /// <summary>
        /// Логическая операция между элементами коллекции.
        /// </summary>
        public EntityLogicalOperation LogicalOperation { get; set; } = EntityLogicalOperation.And;

        /// <summary>
        /// Количество прямых leaf-фильтров в коллекции.
        /// </summary>
        public int Count => Items.Count;

        /// <summary>
        /// Прямые leaf-фильтры коллекции.
        /// </summary>
        internal IReadOnlyList<EntityQueryFilter> Items => _nodes.OfType<EntityQueryFilter>().ToArray();

        /// <summary>
        /// Все прямые элементы коллекции: leaf-фильтры и вложенные группы.
        /// </summary>
        internal IReadOnlyList<EntityQueryFilterNode> Nodes => _nodes;

        #endregion Properties

        #region Add Methods

        /// <summary>
        /// Добавить фильтр сравнения.
        /// </summary>
        /// <param name="path"> ORM-путь колонки. </param>
        /// <param name="comparisonType"> Тип сравнения. </param>
        /// <param name="value"> Значение фильтра. </param>
        /// <returns> Добавленный фильтр. </returns>
        public EntityQueryFilter Add(string path, EntityComparisonType comparisonType, object? value = null)
        {
            var filter = new EntityQueryFilter(path, comparisonType).WithValue(value);
            _nodes.Add(filter);
            return filter;
        }

        /// <summary>
        /// Добавить фильтр поиска по вхождению.
        /// </summary>
        public EntityQueryFilter AddContains(string path, object? value) => Add(path, EntityComparisonType.Contains, value);

        /// <summary>
        /// Добавить фильтр поиска по началу строки.
        /// </summary>
        public EntityQueryFilter AddStartsWith(string path, object? value) => Add(path, EntityComparisonType.StartsWith, value);

        /// <summary>
        /// Добавить фильтр поиска по концу строки.
        /// </summary>
        public EntityQueryFilter AddEndsWith(string path, object? value) => Add(path, EntityComparisonType.EndsWith, value);

        /// <summary>
        /// Добавить фильтр диапазона.
        /// </summary>
        /// <param name="path"> ORM-путь колонки. </param>
        /// <param name="from"> Нижняя граница. </param>
        /// <param name="to"> Верхняя граница. </param>
        /// <returns> Добавленный фильтр. </returns>
        public EntityQueryFilter AddBetween(string path, object? from, object? to)
        {
            var filter = new EntityQueryFilter(path, EntityComparisonType.Equal).WithRange(from, to);
            _nodes.Add(filter);
            return filter;
        }

        /// <summary>
        /// Добавить уже созданный фильтр.
        /// </summary>
        /// <param name="filter"> Фильтр. </param>
        /// <returns> Добавленный фильтр. </returns>
        public EntityQueryFilter Add(EntityQueryFilter filter)
        {
            ArgumentNullException.ThrowIfNull(filter);
            _nodes.Add(filter);
            return filter;
        }

        /// <summary>
        /// Добавить вложенную группу фильтров.
        /// </summary>
        /// <param name="group"> Группа фильтров. </param>
        /// <returns> Добавленная группа. </returns>
        public EntityQueryFilterCollection Add(EntityQueryFilterCollection group)
        {
            ArgumentNullException.ThrowIfNull(group);
            _nodes.Add(group);
            return group;
        }

        /// <summary>
        /// Создать и добавить вложенную группу фильтров.
        /// </summary>
        /// <param name="logicalOperation"> Логическая операция между фильтрами группы. </param>
        /// <returns> Новая группа фильтров. </returns>
        public EntityQueryFilterCollection AddGroup(EntityLogicalOperation logicalOperation = EntityLogicalOperation.And)
        {
            var group = new EntityQueryFilterCollection
            {
                LogicalOperation = logicalOperation
            };
            _nodes.Add(group);
            return group;
        }

        /// <summary>
        /// Добавить фильтр IS NULL.
        /// </summary>
        /// <param name="path"> ORM-путь колонки. </param>
        /// <returns> Добавленный фильтр. </returns>
        public EntityQueryFilter AddIsNull(string path)
        {
            var filter = new EntityQueryFilter(path, EntityComparisonType.IsNull);
            _nodes.Add(filter);
            return filter;
        }

        /// <summary>
        /// Добавить фильтр IS NOT NULL.
        /// </summary>
        /// <param name="path"> ORM-путь колонки. </param>
        /// <returns> Добавленный фильтр. </returns>
        public EntityQueryFilter AddIsNotNull(string path)
        {
            var filter = new EntityQueryFilter(path, EntityComparisonType.IsNotNull);
            _nodes.Add(filter);
            return filter;
        }

        #endregion Add Methods

        #region IEnumerable

        /// <inheritdoc />
        public IEnumerator<EntityQueryFilter> GetEnumerator() => Items.GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion IEnumerable
    }
}
