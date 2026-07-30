using System.Text.Json.Serialization;

namespace Titanic.Entity.Events
{
    /// <summary>
    /// Сообщение событийного transport-слоя для обмена по WebSocket.
    /// </summary>
    public sealed class EntityEventWebSocketRequest
    {
        #region Members

        /// <summary>
        /// Команда, которую должен выполнить listener API.
        /// </summary>
        public EntityEventWebSocketAction Action { get; set; } = EntityEventWebSocketAction.Dispatch;

        /// <summary>
        /// Dispatch-запрос событийного слоя.
        /// </summary>
        public EntityEventDispatchRequest Request { get; set; } = new();

        #endregion Members
    }
}
