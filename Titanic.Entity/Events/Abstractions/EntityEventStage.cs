namespace Titanic.Entity.Events
{
    /// <summary>
    /// Стадия жизненного цикла Entity ORM операции.
    /// </summary>
    public enum EntityEventStage
    {
        #region Members

        /// <summary>
        /// Перед общей операцией сохранения сущности.
        /// </summary>
        Saving = 1,

        /// <summary>
        /// После общей операции сохранения сущности.
        /// </summary>
        Saved = 2,

        /// <summary>
        /// Перед вставкой новой сущности.
        /// </summary>
        Inserting = 3,

        /// <summary>
        /// После вставки новой сущности.
        /// </summary>
        Inserted = 4,

        /// <summary>
        /// Перед обновлением существующей сущности.
        /// </summary>
        Updating = 5,

        /// <summary>
        /// После обновления существующей сущности.
        /// </summary>
        Updated = 6,

        /// <summary>
        /// Перед удалением сущности.
        /// </summary>
        Deleting = 7,

        /// <summary>
        /// После удаления сущности.
        /// </summary>
        Deleted = 8

        #endregion Members
    }
}
