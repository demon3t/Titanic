using Titanic.Db.Enums;

namespace Titanic.Entity.Strurture
{
    /// <summary>
    /// Колонка сущности.
    /// </summary>
    internal class ColumnStructure
    {
        /// <summary>
        /// Название свойства в CLR типе.
        /// </summary>
        public string PropertyName { get; set; } = string.Empty;

        /// <summary>
        /// Назвазвание колонки в БД.
        /// </summary>
        public string ColumnName { get; set; } = string.Empty;

        /// <summary>
        /// Тип колонки в БД.
        /// </summary>
        public DataValueType DataValueType { get; set; }

        /// <summary>
        /// Допускает null значение.
        /// </summary>
        public bool IsNullable { get; set; } = true;

        /// <summary>
        /// Первичная колонка.
        /// </summary>
        public bool IsPrimary { get; set; } = false;

        /// <summary>
        /// Отображаемая колонка.
        /// </summary>
        public bool IsDisplay { get; set; } = false;

        /// <summary>
        /// Локализуемая текстовая колонка.
        /// </summary>
        public bool IsLocalized { get; set; } = false;

        /// <summary>
        /// Принудительно отключить локализацию колонки.
        /// </summary>
        public bool IsLocalizationDisabled { get; set; } = false;

        /// <summary>
        /// Ссылочная колонка.
        /// </summary>
        public bool IsReference => !string.IsNullOrEmpty(ReferenceTabmeName);

        /// <summary>
        /// На какую таблицу ссылается.
        /// </summary>
        public string? ReferenceTableName { get; set; }

        /// <summary>
        /// Обратная совместимость для старого имени свойства.
        /// </summary>
        public string? ReferenceTabmeName
        {
            get => ReferenceTableName;
            set => ReferenceTableName = value;
        }

        /// <summary>
        /// Проверить совпадение по CLR-свойству или имени колонки БД.
        /// </summary>
        public bool Matches(string name)
        {
            return string.Equals(PropertyName, name, StringComparison.OrdinalIgnoreCase)
                || string.Equals(ColumnName, name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
