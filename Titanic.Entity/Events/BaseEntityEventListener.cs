namespace Titanic.Entity.Events
{
    /// <summary>
    /// Базовый обработчик событий Entity ORM.
    /// </summary>
    public abstract class BaseEntityEventListener
    {
        #region Members

        /// <summary>
        /// Вызывается перед общей операцией сохранения сущности.
        /// </summary>
        /// <param name="entity"> Текущая ORM-сущность. </param>
        /// <param name="args"> Аргументы события. </param>
        public virtual void OnSaving(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
        {
            ValidateArguments(entity, args);
        }

        /// <summary>
        /// Вызывается после общей операции сохранения сущности.
        /// </summary>
        /// <param name="entity"> Текущая ORM-сущность. </param>
        /// <param name="args"> Аргументы события. </param>
        public virtual void OnSaved(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
        {
            ValidateArguments(entity, args);
        }

        /// <summary>
        /// Вызывается перед вставкой новой сущности.
        /// </summary>
        /// <param name="entity"> Текущая ORM-сущность. </param>
        /// <param name="args"> Аргументы события. </param>
        public virtual void OnInserting(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
        {
            ValidateArguments(entity, args);
        }

        /// <summary>
        /// Вызывается после вставки новой сущности.
        /// </summary>
        /// <param name="entity"> Текущая ORM-сущность. </param>
        /// <param name="args"> Аргументы события. </param>
        public virtual void OnInserted(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
        {
            ValidateArguments(entity, args);
        }

        /// <summary>
        /// Вызывается перед обновлением существующей сущности.
        /// </summary>
        /// <param name="entity"> Текущая ORM-сущность. </param>
        /// <param name="args"> Аргументы события. </param>
        public virtual void OnUpdating(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
        {
            ValidateArguments(entity, args);
        }

        /// <summary>
        /// Вызывается после обновления существующей сущности.
        /// </summary>
        /// <param name="entity"> Текущая ORM-сущность. </param>
        /// <param name="args"> Аргументы события. </param>
        public virtual void OnUpdated(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
        {
            ValidateArguments(entity, args);
        }

        /// <summary>
        /// Вызывается перед удалением сущности.
        /// </summary>
        /// <param name="entity"> Текущая ORM-сущность. </param>
        /// <param name="args"> Аргументы события. </param>
        public virtual void OnDeleting(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
        {
            ValidateArguments(entity, args);
        }

        /// <summary>
        /// Вызывается после удаления сущности.
        /// </summary>
        /// <param name="entity"> Текущая ORM-сущность. </param>
        /// <param name="args"> Аргументы события. </param>
        public virtual void OnDeleted(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
        {
            ValidateArguments(entity, args);
        }

        /// <summary>
        /// Проверить аргументы обработчика событий Entity ORM.
        /// </summary>
        /// <param name="entity"> Текущая ORM-сущность. </param>
        /// <param name="args"> Аргументы события. </param>
        private static void ValidateArguments(global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
        {
            ArgumentNullException.ThrowIfNull(entity);
            ArgumentNullException.ThrowIfNull(args);
        }

        #endregion Members
    }
}
