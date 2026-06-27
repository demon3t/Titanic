namespace Titanic.Entity.Events
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Контракт ответа dispatch-вызова обработчика событий.
    /// </summary>
    public sealed class EntityEventDispatchResponse
    {
        #region Members

        /// <summary>
        /// Признак успешного завершения обработки события.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Признак отмены pipeline обработчиком.
        /// </summary>
        public bool Canceled { get; set; }

        /// <summary>
        /// Причина отмены, возвращённая обработчиком.
        /// </summary>
        public string? CancelReason { get; set; }

        /// <summary>
        /// Сообщение об ошибке, возвращённое обработчиком.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Итоговые значения сущности после обработки события.
        /// </summary>
        [JsonConverter(typeof(EntityEventTransportValueDictionaryJsonConverter))]
        public Dictionary<string, object?> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Полный итоговый снимок сущности после обработки события.
        /// </summary>
        public EntityEventEntitySnapshot? Entity { get; set; }

        #endregion Members
    }
}
