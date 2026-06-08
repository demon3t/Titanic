namespace Titanic.Entity.Events
{
    /// <summary>
    /// Стандартные маршруты и настройки API событийного слоя.
    /// </summary>
    public static class EntityEventListenerApiDefaults
    {
        #region Members

        /// <summary>
        /// HTTP endpoint dispatch-вызова событийного слоя.
        /// </summary>
        public const string HttpDispatchPath = "/entity-event-listener/dispatch";

        #endregion Members
    }
}
