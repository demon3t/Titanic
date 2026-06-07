using Titanic.Db.Enums;

namespace Titanic.Entity.WebApplication.Api
{
    /// <summary>
    /// HTTP-модель колонки сущности из структуры менеджера.
    /// </summary>
    public sealed class EntityColumnStructureResponse
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
        /// Тип значения колонки.
        /// </summary>
        public DataValueType DataValueType { get; set; }

        /// <summary>
        /// Допускает ли колонка null.
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// Признак первичной колонки.
        /// </summary>
        public bool IsPrimary { get; set; }

        /// <summary>
        /// Признак отображаемой колонки.
        /// </summary>
        public bool IsDisplay { get; set; }

        /// <summary>
        /// Признак локализуемой колонки.
        /// </summary>
        public bool IsLocalized { get; set; }

        /// <summary>
        /// Признак ссылочной колонки.
        /// </summary>
        public bool IsReference { get; set; }

        /// <summary>
        /// Имя таблицы, на которую ссылается колонка.
        /// </summary>
        public string? ReferenceTableName { get; set; }
    }
}
