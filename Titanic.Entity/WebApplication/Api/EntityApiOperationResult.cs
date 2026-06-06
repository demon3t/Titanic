using System.Text.Json.Serialization;

namespace Titanic.Entity.WebApplication.Api
{
    /// <summary>
    /// Результат выполнения одной операции Entity API.
    /// </summary>
    public sealed class EntityApiOperationResult
    {
        /// <summary>
        /// Строковое имя операции внутри batch-запроса.
        /// Позволяет фронту сопоставить ответ с исходным запросом.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Тип выполненной операции.
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public EntityApiOperationType Operation { get; set; }

        /// <summary>
        /// Признак успешного выполнения операции.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// HTTP-статус результата операции.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Данные результата операции.
        /// </summary>
        public object? Result { get; set; }

        /// <summary>
        /// Текст ошибки, если операция завершилась неуспешно.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Создать успешный результат операции.
        /// </summary>
        /// <param name="operation"> Тип операции. </param>
        /// <param name="result"> Данные результата. </param>
        /// <returns> Результат операции. </returns>
        public static EntityApiOperationResult Ok(EntityApiOperationType operation, object? result, string? name = null)
        {
            return new EntityApiOperationResult
            {
                Name = name,
                Operation = operation,
                Success = true,
                StatusCode = 200,
                Result = result
            };
        }

        /// <summary>
        /// Создать неуспешный результат операции.
        /// </summary>
        /// <param name="operation"> Тип операции. </param>
        /// <param name="statusCode"> HTTP-статус. </param>
        /// <param name="message"> Текст ошибки. </param>
        /// <returns> Результат операции. </returns>
        public static EntityApiOperationResult Fail(
            EntityApiOperationType operation,
            int statusCode,
            string message,
            string? name = null)
        {
            return new EntityApiOperationResult
            {
                Name = name,
                Operation = operation,
                Success = false,
                StatusCode = statusCode,
                ErrorMessage = message
            };
        }
    }
}

