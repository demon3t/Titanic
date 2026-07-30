using System.Text.Json.Serialization;
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
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public EntityEventStage Stage { get; set; }

        /// <summary>
        /// Последовательность стадий, которую серверный listener должен выполнить в рамках одного transport-вызова.
        /// </summary>
        public List<EntityEventStage> Stages { get; set; } = [];

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
        [JsonConverter(typeof(EntityEventTransportValueDictionaryJsonConverter))]
        public Dictionary<string, object?> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Старые значения колонок сущности до текущей операции.
        /// </summary>
        [JsonConverter(typeof(EntityEventTransportValueDictionaryJsonConverter))]
        public Dictionary<string, object?> OldValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Полный снимок сущности для точного восстановления в удалённом listener-е.
        /// </summary>
        public EntityEventEntitySnapshot? Entity { get; set; }

        #endregion Members
    }
}
