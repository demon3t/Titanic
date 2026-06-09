namespace Titanic.Entity.Events
{
    /// <summary>
    /// Стандартные маршруты и настройки API событийного слоя.
    /// </summary>
    public static class EntityEventListenerApiDefaults
    {
        #region Members

        /// <summary>
        /// Базовый HTTP-путь API обработчика событий.
        /// </summary>
        public const string HttpBasePath = "/entity-event-listener";

        /// <summary>
        /// Legacy HTTP endpoint общего dispatch-вызова событийного слоя.
        /// </summary>
        public const string HttpDispatchPath = "/entity-event-listener/dispatch";

        /// <summary>
        /// HTTP-действие создания экземпляра remote listener-а.
        /// </summary>
        public const string HttpCreateActionPath = "create";

        /// <summary>
        /// HTTP-действие вызова стадии Saving.
        /// </summary>
        public const string HttpOnSavingActionPath = "on-saving";

        /// <summary>
        /// HTTP-действие вызова стадии Saved.
        /// </summary>
        public const string HttpOnSavedActionPath = "on-saved";

        /// <summary>
        /// HTTP-действие вызова стадии Inserting.
        /// </summary>
        public const string HttpOnInsertingActionPath = "on-inserting";

        /// <summary>
        /// HTTP-действие вызова стадии Inserted.
        /// </summary>
        public const string HttpOnInsertedActionPath = "on-inserted";

        /// <summary>
        /// HTTP-действие вызова стадии Updating.
        /// </summary>
        public const string HttpOnUpdatingActionPath = "on-updating";

        /// <summary>
        /// HTTP-действие вызова стадии Updated.
        /// </summary>
        public const string HttpOnUpdatedActionPath = "on-updated";

        /// <summary>
        /// HTTP-действие вызова стадии Deleting.
        /// </summary>
        public const string HttpOnDeletingActionPath = "on-deleting";

        /// <summary>
        /// HTTP-действие вызова стадии Deleted.
        /// </summary>
        public const string HttpOnDeletedActionPath = "on-deleted";

        /// <summary>
        /// HTTP-действие удаления экземпляра remote listener-а.
        /// </summary>
        public const string HttpDeleteActionPath = "delete";

        /// <summary>
        /// Возвращает HTTP-действие для указанной стадии событийного pipeline.
        /// </summary>
        /// <param name="stage">Стадия событийного pipeline.</param>
        /// <returns>Относительный HTTP-путь действия.</returns>
        public static string GetHttpActionPath(EntityEventStage stage)
        {
            return stage switch
            {
                EntityEventStage.Saving => HttpOnSavingActionPath,
                EntityEventStage.Saved => HttpOnSavedActionPath,
                EntityEventStage.Inserting => HttpOnInsertingActionPath,
                EntityEventStage.Inserted => HttpOnInsertedActionPath,
                EntityEventStage.Updating => HttpOnUpdatingActionPath,
                EntityEventStage.Updated => HttpOnUpdatedActionPath,
                EntityEventStage.Deleting => HttpOnDeletingActionPath,
                EntityEventStage.Deleted => HttpOnDeletedActionPath,
                _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unsupported entity event stage.")
            };
        }

        /// <summary>
        /// Проверяет, является ли стадия началом обработки одной сущности.
        /// </summary>
        /// <param name="stage">Стадия событийного pipeline.</param>
        /// <returns><see langword="true" />, если перед стадией нужно создать remote listener.</returns>
        public static bool IsInitialStage(EntityEventStage stage)
        {
            return stage is EntityEventStage.Saving or EntityEventStage.Deleting;
        }

        /// <summary>
        /// Проверяет, является ли стадия завершением обработки одной сущности.
        /// </summary>
        /// <param name="stage">Стадия событийного pipeline.</param>
        /// <returns><see langword="true" />, если после стадии нужно удалить remote listener.</returns>
        public static bool IsFinalStage(EntityEventStage stage)
        {
            return stage is EntityEventStage.Saved or EntityEventStage.Deleted;
        }

        #endregion Members
    }
}
