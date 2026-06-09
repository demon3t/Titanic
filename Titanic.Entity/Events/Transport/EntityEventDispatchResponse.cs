namespace Titanic.Entity.Events
{
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
        public Dictionary<string, object?> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        #endregion Members
    }
}
