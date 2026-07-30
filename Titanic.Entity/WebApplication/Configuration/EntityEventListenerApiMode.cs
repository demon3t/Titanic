namespace Titanic.Entity.WebApplication.Configuration
{
    /// <summary>
    /// Режим публикации API обработчика событий для менеджера.
    /// </summary>
    public enum EntityEventListenerApiMode
    {
        #region Members

        /// <summary>
        /// Документирует член типа.
        /// </summary>
        None = 0,

        /// <summary>
        /// Документирует член типа.
        /// </summary>
        Http = 1,

        /// <summary>
        /// Документирует член типа.
        /// </summary>
        Grpc = 2,

        /// <summary>
        /// Документирует член типа.
        /// </summary>
        WebSocket = 3

        #endregion Members
    }
}
