using Titanic.Common.Session;

namespace Titanic.Entity.Events
{
    /// <summary>
    /// Контракт dispatch-запроса событийного слоя.
    /// </summary>
    public sealed class EntityEventDispatchRequest
    {
        #region Members

        /// <summary>
        /// Имя Entity ORM менеджера.
        /// </summary>
        public string ManagerName { get; set; } = string.Empty;

        /// <summary>
        /// Имя таблицы сущности.
        /// </summary>
        public string TableName { get; set; } = string.Empty;

        /// <summary>
        /// Идентификатор обработки одной ORM-сущности во внешнем listener-е.
        /// </summary>
        public string DispatchId { get; set; } = string.Empty;

        /// <summary>
        /// Этап событийного pipeline.
        /// </summary>
        public string Stage { get; set; } = string.Empty;

        /// <summary>
        /// Признак новой сущности.
        /// </summary>
        public bool IsNew { get; set; }

        /// <summary>
        /// Контекст пользователя.
        /// </summary>
        public UserConnection UserConnection { get; set; } = new();

        /// <summary>
        /// Значения колонок сущности.
        /// </summary>
        public Dictionary<string, object?> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        #endregion Members
    }
}
