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
        /// Возвращает или задаёт путь.
        /// </summary>
        public string Path { get; set; } = "/entity-event-listener";

        #endregion Members
    }
}
