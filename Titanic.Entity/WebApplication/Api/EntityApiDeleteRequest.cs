namespace Titanic.Entity.WebApplication.Api
{
    /// <summary>
    /// HTTP-модель удаления сущности.
    /// </summary>
    public sealed class EntityApiDeleteRequest
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
        /// Значения первичного ключа и других колонок, необходимых для materialization Entity.
        /// </summary>
        public Dictionary<string, object?> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
