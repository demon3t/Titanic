using System.Text.Json.Serialization;

namespace Titanic.Entity.Events
{
    /// <summary>
    /// Полный transport-снимок ORM-сущности для удалённого событийного listener-а.
    /// </summary>
    public sealed class EntityEventEntitySnapshot
    {
        #region Members

        /// <summary>
        /// Имя таблицы корневой сущности.
        /// </summary>
        public string TableName { get; set; } = string.Empty;

        /// <summary>
        /// Признак новой сущности.
        /// </summary>
        public bool IsNew { get; set; }

        /// <summary>
        /// Полный словарь ORM-путей и соответствующих им алиасов.
        /// </summary>
        public Dictionary<string, string> Paths { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Полный словарь значений колонок по алиасам.
        /// </summary>
        public Dictionary<string, EntityEventColumnSnapshot> Columns { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Снимок старых значений сущности до текущей операции.
        /// </summary>
        [JsonConverter(typeof(EntityEventTransportValueDictionaryJsonConverter))]
        public Dictionary<string, object?> OldValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        #endregion Members
    }
}
