namespace Titanic.Entity.WebApplication.Api
{
    /// <summary>
    /// HTTP-модель создания или обновления сущности.
    /// </summary>
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
