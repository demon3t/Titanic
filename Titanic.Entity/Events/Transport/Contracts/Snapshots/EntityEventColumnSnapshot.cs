using System.Text.Json.Serialization;

namespace Titanic.Entity.Events
{
    /// <summary>
    /// Снимок одного значения колонки для передачи Entity через транспорт событийного слоя.
    /// </summary>
    public sealed class EntityEventColumnSnapshot
    {
        #region Members

        /// <summary>
        /// Алиас значения внутри ORM-сущности.
        /// </summary>
        public string Alias { get; set; } = string.Empty;

        /// <summary>
        /// Тип значения колонки в терминах <see cref="Titanic.Db.Enums.DataValueType" />.
        /// </summary>
        public int DataValueType { get; set; }

        /// <summary>
        /// Признак ссылочной колонки.
        /// </summary>
        public bool IsReference { get; set; }

        /// <summary>
        /// Сырое значение колонки.
        /// </summary>
        [JsonConverter(typeof(EntityEventTransportValueJsonConverter))]
        public object? Value { get; set; }

        /// <summary>
        /// Отображаемое значение колонки.
        /// </summary>
        [JsonConverter(typeof(EntityEventTransportValueJsonConverter))]
        public object? DisplayValue { get; set; }

        #endregion Members
    }
}
