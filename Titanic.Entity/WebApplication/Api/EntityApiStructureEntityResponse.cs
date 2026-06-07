namespace Titanic.Entity.WebApplication.Api
{
    /// <summary>
    /// HTTP-модель структуры сущности Entity API.
    /// </summary>
    public sealed class EntityApiStructureEntityResponse
    {
        /// <summary>
        /// Имя таблицы.
        /// </summary>
        public string TableName { get; set; } = string.Empty;

        /// <summary>
        /// Полное имя CLR-типа сущности.
        /// </summary>
        public string EntityTypeName { get; set; } = string.Empty;

        /// <summary>
        /// Колонки сущности.
        /// </summary>
        public List<EntityApiStructureColumnResponse> Columns { get; set; } = [];
    }
}
