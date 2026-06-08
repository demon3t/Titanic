namespace Titanic.Entity.Events
{
    /// <summary>
    /// Стадия жизненного цикла Entity ORM операции.
    /// </summary>
    public enum EntityEventStage
    {
        #region Members

        Saving = 1,
        Saved = 2,
        Inserting = 3,
        Inserted = 4,
        Updating = 5,
        Updated = 6,
        Deleting = 7,
        Deleted = 8

        #endregion Members
    }
}
