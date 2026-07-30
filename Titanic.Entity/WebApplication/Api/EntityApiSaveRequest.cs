namespace Titanic.Entity.WebApplication.Api
{
    /// <summary>
    /// Legacy HTTP-модель создания или обновления сущности.
    /// </summary>
    [Obsolete("Deprecated; RemoveIn=1.4.0; Replacement=EntityApiRequest with Operation = EntityApiOperationType.Save")]
    public sealed class EntityApiSaveRequest
    {
        /// <summary>
        /// Имя таблицы сущности.
        /// </summary>
        public string? TableName { get; set; }

        /// <summary>
        /// Имя CLR-типа сущности.
        /// </summary>
        public string? EntityTypeName { get; set; }

        /// <summary>
        /// Значения колонок по ORM-путям, именам колонок или SQL-алиасам.
        /// </summary>
        public Dictionary<string, object?> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
