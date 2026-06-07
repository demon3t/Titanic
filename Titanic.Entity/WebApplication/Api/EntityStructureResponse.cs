namespace Titanic.Entity.WebApplication.Api
{
    /// <summary>
    /// HTTP-модель одной сущности из структуры менеджера.
    /// </summary>
    public sealed class EntityStructureResponse
    {
        /// <summary>
        /// Полное имя CLR-типа.
        /// </summary>
        public string EntityTypeName { get; set; } = string.Empty;

        /// <summary>
        /// Короткое имя CLR-типа.
        /// </summary>
        public string EntityTypeShortName { get; set; } = string.Empty;

        /// <summary>
        /// Имя таблицы.
        /// </summary>
        public string TableName { get; set; } = string.Empty;

        /// <summary>
        /// Признак представления.
        /// </summary>
        public bool IsView { get; set; }

        /// <summary>
        /// Признак отключённой локализации.
        /// </summary>
        public bool IsLocalizationDisabled { get; set; }

        /// <summary>
        /// Колонки сущности.
        /// </summary>
        public List<EntityColumnStructureResponse> Columns { get; set; } = [];
    }
}
