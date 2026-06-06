namespace Titanic.Entity.Orm
{
    /// <summary>
    /// Описание колонки ORM-запроса.
    /// </summary>
    public sealed class EntityQueryColumn
    {
        internal EntityQueryColumn(
            string path,
            string? alias = null,
            EntityAggregationType aggregationType = EntityAggregationType.None)
        {
            Path = path;
            Alias = alias;
            AggregationType = aggregationType;
        }

        /// <summary>
        /// ORM-путь колонки.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Пользовательский алиас результата.
        /// </summary>
        public string? Alias { get; }

        /// <summary>
        /// Тип агрегатной функции.
        /// </summary>
        public EntityAggregationType AggregationType { get; }
    }
}
