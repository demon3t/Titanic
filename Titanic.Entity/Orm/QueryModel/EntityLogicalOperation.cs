namespace Titanic.Entity.Orm
{
    /// <summary>
    /// Логическая операция для объединения ORM-фильтров.
    /// </summary>
    public enum EntityLogicalOperation
    {
        /// <summary>
        /// Объединять через AND.
        /// </summary>
        And = 0,

        /// <summary>
        /// Объединять через OR.
        /// </summary>
        Or = 1
    }
}
