using System.Text.Json.Serialization;

namespace Titanic.Entity.WebApplication.Api
{
    /// <summary>
    /// HTTP-модель ошибки Entity API.
    /// </summary>
    public sealed class EntityApiErrorResponse
    {
        /// <summary>
        /// Текст ошибки.
        /// </summary>
        public string Error { get; set; } = string.Empty;

        /// <summary>
        /// HTTP-статус ответа.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Тип операции, если ошибка относится к обычному Entity API запросу.
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public EntityApiOperationType? Operation { get; set; }

        /// <summary>
        /// Имя операции внутри batch-запроса.
        /// </summary>
        public string? Name { get; set; }
    }
}
