namespace Titanic.Entity.WebApplication.Api
{
    /// <summary>
    /// Legacy HTTP-модель создания или обновления сущности.
    /// </summary>
    [Obsolete("Use EntityApiRequest with Operation = EntityApiOperationType.Save instead. This type will be removed in 1.4.0.")]
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
