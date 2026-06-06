using System.Text.Json.Serialization;

namespace Titanic.Entity.WebApplication.Api
{
    /// <summary>
    /// HTTP-модель batch-запроса Entity API.
    /// </summary>
    public sealed class EntityApiBatchRequest
    {
        /// <summary>
        /// Режим обработки операций. Если не задан, используется настройка менеджера.
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public EntityApiBatchExecutionMode? ExecutionMode { get; set; }

        /// <summary>
        /// Операции, которые нужно выполнить.
        /// </summary>
        public List<EntityApiRequest> Requests { get; set; } = [];
    }
}

