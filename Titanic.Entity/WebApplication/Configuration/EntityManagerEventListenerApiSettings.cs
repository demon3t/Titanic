namespace Titanic.Entity.WebApplication.Configuration
{
    /// <summary>
    /// Настройки публикации API обработчика событий для конкретного менеджера.
    /// </summary>
    public sealed class EntityManagerEventListenerApiSettings
    {
        #region Members

        /// <summary>
        /// Режим публикации API обработчика событий.
        /// </summary>
        public EntityEventListenerApiMode Mode { get; set; } = EntityEventListenerApiMode.None;

        /// <summary>
        /// HTTP-путь endpoint-а обработчика событий.
        /// </summary>
        public string Path { get; set; } = "/entity-event-listener";

        /// <summary>
        /// Время хранения экземпляра remote listener-а без новых запросов по той же сущности.
        /// </summary>
        public TimeSpan ListenerInstanceIdleTimeout { get; set; } = TimeSpan.FromMinutes(5);

        #endregion Members
    }
}
