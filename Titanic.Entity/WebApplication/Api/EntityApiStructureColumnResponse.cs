using Titanic.Db.Enums;

namespace Titanic.Entity.WebApplication.Api
{
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
