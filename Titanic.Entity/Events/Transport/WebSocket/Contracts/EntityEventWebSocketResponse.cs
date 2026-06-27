namespace Titanic.Entity.Events
{
    /// <summary>
    /// Ответ listener API на WebSocket-команду событийного transport-слоя.
    /// </summary>
    public sealed class EntityEventWebSocketResponse
    {
        #region Members

        /// <summary>
        /// Команда, на которую был сформирован ответ.
        /// </summary>
        public EntityEventWebSocketAction Action { get; set; } = EntityEventWebSocketAction.Dispatch;

        /// <summary>
        /// Результат обработки transport-запроса.
        /// </summary>
        public EntityEventDispatchResponse Response { get; set; } = new();

        #endregion Members
    }
}
