using Titanic.Db.Enums;

namespace Titanic.Entity.WebApplication.Api
{
    /// <summary>
    /// HTTP-модель структуры Entity API менеджера.
    /// </summary>
    public sealed class EntityApiManagerStructureResponse
    {
        /// <summary>
        /// Имя менеджера.
        /// </summary>
        public string ManagerName { get; set; } = string.Empty;

        /// <summary>
        /// Доступные сущности менеджера.
        /// </summary>
        public List<EntityApiStructureEntityResponse> Entities { get; set; } = [];
    }

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

    /// <summary>
    /// HTTP-модель структуры колонки Entity API.
    /// </summary>
    public sealed class EntityApiStructureColumnResponse
    {
        /// <summary>
        /// Имя CLR-свойства.
        /// </summary>
        public string PropertyName { get; set; } = string.Empty;

        /// <summary>
        /// Имя колонки в БД.
        /// </summary>
        public string ColumnName { get; set; } = string.Empty;

        /// <summary>
        /// Тип значения.
        /// </summary>
        public DataValueType DataValueType { get; set; }

        /// <summary>
        /// Признак nullable-колонки.
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// Признак primary key.
        /// </summary>
        public bool IsPrimary { get; set; }

        /// <summary>
        /// Признак display-колонки.
        /// </summary>
        public bool IsDisplay { get; set; }

        /// <summary>
        /// Признак reference-колонки.
        /// </summary>
        public bool IsReference { get; set; }

        /// <summary>
        /// Таблица ссылки, если колонка reference.
        /// </summary>
        public string? ReferenceTableName { get; set; }
    }
}
