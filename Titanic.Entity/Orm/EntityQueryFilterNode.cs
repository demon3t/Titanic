namespace Titanic.Entity.Orm
{
    /// <summary>
    /// Базовый элемент дерева фильтров EntitySchemaQuery.
    /// </summary>
    public abstract class EntityQueryFilterNode
    {
        /// <summary>
        /// Признак активного элемента фильтра.
        /// </summary>
        public bool IsEnabled { get; set; } = true;
    }
}
