using System.Text.Json;
using System.Text.Json.Serialization;

namespace Titanic.Entity.Events
{
    /// <summary>
    /// Общие JSON-настройки transport-сообщений событийного слоя для WebSocket.
    /// </summary>
    internal static class EntityEventWebSocketSerializer
    {
        #region Fields

        /// <summary>
        /// Единые JSON-настройки для сериализации transport-команд и ответов WebSocket-канала.
        /// </summary>
        internal static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };

        #endregion Fields
    }
}
