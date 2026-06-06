using Titanic.Entity.Orm;
using System.Text.Json.Serialization;

namespace Titanic.Entity.WebApplication.Api
{
    /// <summary>
    /// Единая HTTP-модель операции Entity API.
    /// </summary>
    public sealed class EntityApiRequest
    {
        /// <summary>
        /// Строковое имя операции внутри batch-запроса.
        /// Если не задано, backend назначит Guid перед выполнением batch.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Тип операции, которую должен выполнить API.
        /// </summary>
        [JsonConverter(typeof(EntityApiOperationTypeJsonConverter))]
        public EntityApiOperationType Operation { get; set; }

        /// <summary>
        /// ESQ-модель для операции Select.
        /// </summary>
        public ESQJsonModel? Query { get; set; }

        /// <summary>
        /// Имя таблицы сущности для операций Save и Delete.
        /// </summary>
        public string? TableName { get; set; }

        /// <summary>
        /// Имя CLR-типа сущности для операций Save и Delete.
        /// </summary>
        public string? EntityTypeName { get; set; }

        /// <summary>
        /// Значения колонок по ORM-путям, именам колонок или SQL-алиасам.
        /// </summary>
        public Dictionary<string, object?> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}

