using System.Text.Json.Serialization;

namespace Titanic.Entity.Events
{
    /// <summary>
    /// Тип transport-команды событийного слоя, передаваемой по WebSocket.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum EntityEventWebSocketAction
    {
        #region Members

        /// <summary>
        /// Лениво создаёт экземпляр listener-а для DispatchId.
        /// </summary>
        Create = 0,

        /// <summary>
        /// Выполняет dispatch-запрос с учётом стадии, указанной в request.
        /// </summary>
        Dispatch = 1,

        /// <summary>
        /// Выполняет конкретную стадию pipeline.
        /// </summary>
        ExecuteStage = 2,

        /// <summary>
        /// Освобождает cached listener для DispatchId.
        /// </summary>
        Delete = 3

        #endregion Members
    }
}
