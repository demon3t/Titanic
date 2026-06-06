using System.Text.Json.Serialization;

namespace Titanic.Entity.WebApplication.Api
{
    /// <summary>
    /// Результат batch-запроса Entity API.
    /// </summary>
    public sealed class EntityApiBatchResponse
    {
        /// <summary>
        /// Режим, в котором был обработан batch-запрос.
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public EntityApiBatchExecutionMode ExecutionMode { get; set; }

        /// <summary>
        /// Результаты операций batch-запроса.
        /// </summary>
        public List<EntityApiOperationResult> Results { get; set; } = [];
    }
}

