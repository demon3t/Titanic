using Titanic.Common.Session;
using Titanic.Db.Abstractions;
using Titanic.Entity.Orm;
using Titanic.Entity.Strurture;
using Titanic.Entity.WebApplication.Configuration;

namespace Titanic.Entity.Interfaces
{
    /// <summary>
    /// Базовый класс менеджеров Entity ORM.
    /// </summary>
    public abstract class BaseEntityManager
    {
        #region Properties

        /// <summary>
        /// Возвращает имя менеджера Entity ORM.
        /// </summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// Возвращает провайдер БД, используемый менеджером.
        /// </summary>
        public BaseDbProvider Provider { get; private set; } = null!;

        /// <summary>
        /// Возвращает настройки Entity API для менеджера.
        /// </summary>
        public EntityManagerApiSettings Api { get; private set; } = new();

        /// <summary>
        /// Возвращает runtime-настройки Entity ORM для менеджера.
        /// </summary>
        public EntityManagerOptions Options { get; private set; } = new();

        /// <summary>
        /// Возвращает признак проверки схемы БД при инициализации.
        /// </summary>
        public bool ValidateDatabaseSchemaOnCompile { get; private set; }

        /// <summary>
        /// Возвращает расположение удалённого обработчика событий.
        /// </summary>
        public string? EventListener { get; private set; }

        /// <summary>
        /// Возвращает признак локальной обработки событий.
        /// </summary>
        public bool HasLocalEventListener => string.IsNullOrWhiteSpace(EventListener);

        /// <summary>
        /// Возвращает настройки API обработчика событий для менеджера.
        /// </summary>
        public EntityManagerEventListenerApiSettings EventListenerApi { get; private set; } = new();

        /// <summary>
        /// Возвращает scope структур сущностей, используемый менеджером.
        /// </summary>
        internal EntityStructureScope StructureScope { get; private set; } = Structure.DefaultScope;

        #endregion Properties

        #region Initialization

        /// <summary>
        /// Инициализирует новый экземпляр Initialize.
        /// </summary>
        public virtual void Initialize(string name, BaseDbProvider provider, EntityManagerSettings? settings = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Entity manager name is empty", nameof(name));
            }

            ArgumentNullException.ThrowIfNull(provider);

            Name = name;
            Provider = provider;
            Api = NormalizeApiSettings(settings?.Api);
            Options = settings?.Options ?? new EntityManagerOptions();
            EventListener = NormalizeEventListener(settings?.EventListener);
            EventListenerApi = NormalizeEventListenerApiSettings(name, settings?.EventListenerApi);
            ValidateDatabaseSchemaOnCompile = settings?.ValidateDatabaseSchemaOnCompile ?? false;
            StructureScope = Structure.GetScope(settings?.EntityModelNamespaces);

            if (ValidateDatabaseSchemaOnCompile)
            {
                EntitySchemaValidator.Validate(provider, StructureScope);
            }
        }

        /// <summary>
        /// Создаёт и инициализирует менеджер Entity ORM.
        /// </summary>
        internal static TManager BuildManager<TManager>(string name, BaseDbProvider provider, EntityManagerSettings? settings = null)
            where TManager : BaseEntityManager, new()
        {
            var manager = new TManager();
            manager.Initialize(name, provider, settings);
            return manager;
        }

        #endregion Initialization

        #region Entity Queries

        /// <summary>
        /// Создаёт Entity Schema Query.
        /// </summary>
        public EntitySchemaQuery<TEntity> Query<TEntity>(UserConnection userConnection)
        {
            ArgumentNullException.ThrowIfNull(userConnection);
            return ApplyQuerySettings(new EntitySchemaQuery<TEntity>(Provider, StructureScope, userConnection));
        }

        /// <summary>
        /// Инициализирует новый экземпляр Query.
        /// </summary>
        public EntitySchemaQuery Query(Type entityType, UserConnection userConnection)
        {
            ArgumentNullException.ThrowIfNull(entityType);
            ArgumentNullException.ThrowIfNull(userConnection);
            return ApplyQuerySettings(new EntitySchemaQuery(Provider, StructureScope, entityType, userConnection));
        }

        /// <summary>
        /// Инициализирует новый экземпляр Query.
        /// </summary>
        public EntitySchemaQuery Query(string tableName, UserConnection userConnection)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
            ArgumentNullException.ThrowIfNull(userConnection);
            return ApplyQuerySettings(new EntitySchemaQuery(Provider, StructureScope, tableName, userConnection));
        }

        #endregion Entity Queries

        #region Entity Select Builders

        /// <summary>
        /// Создаёт ORM SELECT builder.
        /// </summary>
        public EntitySelectBuilder<TEntity> Select<TEntity>(UserConnection userConnection)
        {
            ArgumentNullException.ThrowIfNull(userConnection);
            return new EntitySelectBuilder<TEntity>(Provider, StructureScope, userConnection);
        }

        /// <summary>
        /// Инициализирует новый экземпляр Select.
        /// </summary>
        public EntitySelectBuilder Select(Type entityType, UserConnection userConnection)
        {
            ArgumentNullException.ThrowIfNull(entityType);
            ArgumentNullException.ThrowIfNull(userConnection);
            return new EntitySelectBuilder(Provider, StructureScope, entityType, userConnection);
        }

        /// <summary>
        /// Инициализирует новый экземпляр Select.
        /// </summary>
        public EntitySelectBuilder Select(string tableName, UserConnection userConnection)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
            ArgumentNullException.ThrowIfNull(userConnection);
            return new EntitySelectBuilder(Provider, StructureScope, tableName, userConnection);
        }

        #endregion Entity Select Builders

        #region Entity CRUD

        /// <summary>
        /// Создаёт экземпляр ORM-сущности.
        /// </summary>
        public Orm.Entity Create<TEntity>(UserConnection userConnection)
        {
            ArgumentNullException.ThrowIfNull(userConnection);
            return EntityManager.Create(StructureScope.GetEntityStructure(typeof(TEntity)), Provider, userConnection, isNew: true, manager: this);
        }

        /// <summary>
        /// Инициализирует новый экземпляр Create.
        /// </summary>
        public Orm.Entity Create(Type entityType, UserConnection userConnection)
        {
            ArgumentNullException.ThrowIfNull(entityType);
            ArgumentNullException.ThrowIfNull(userConnection);
            return EntityManager.Create(StructureScope.GetEntityStructure(entityType), Provider, userConnection, isNew: true, manager: this);
        }

        /// <summary>
        /// Инициализирует новый экземпляр Create.
        /// </summary>
        public Orm.Entity Create(string tableName, UserConnection userConnection)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
            ArgumentNullException.ThrowIfNull(userConnection);
            return EntityManager.Create(StructureScope.GetEntityStructure(tableName), Provider, userConnection, isNew: true, manager: this);
        }

        /// <summary>
        /// Инициализирует новый экземпляр Create.
        /// </summary>
        public Orm.Entity Create(string tableName, UserConnection userConnection, bool isNew)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
            ArgumentNullException.ThrowIfNull(userConnection);
            return EntityManager.Create(StructureScope.GetEntityStructure(tableName), Provider, userConnection, isNew: isNew, manager: this);
        }

        #endregion Entity CRUD

        #region Private Methods

        /// <summary>
        /// Инициализирует новый экземпляр NormalizeApiSettings.
        /// </summary>
        private static EntityManagerApiSettings NormalizeApiSettings(EntityManagerApiSettings? settings)
        {
            var api = settings ?? new EntityManagerApiSettings();
            api.Path = string.IsNullOrWhiteSpace(api.Path) ? "/entity" : api.Path;
            api.AuthorizationHeaderName = string.IsNullOrWhiteSpace(api.AuthorizationHeaderName)
                ? "X-Entity-Key"
                : api.AuthorizationHeaderName;
            return api;
        }

        /// <summary>
        /// Инициализирует новый экземпляр NormalizeEventListener.
        /// </summary>
        private static string? NormalizeEventListener(string? eventListener)
        {
            return string.IsNullOrWhiteSpace(eventListener)
                ? null
                : eventListener.Trim();
        }

        /// <summary>
        /// Инициализирует новый экземпляр NormalizeEventListenerApiSettings.
        /// </summary>
        private static EntityManagerEventListenerApiSettings NormalizeEventListenerApiSettings(
            string managerName,
            EntityManagerEventListenerApiSettings? settings)
        {
            var api = settings ?? new EntityManagerEventListenerApiSettings();
            if (api.Mode == EntityEventListenerApiMode.Http)
            {
                api.Path = string.IsNullOrWhiteSpace(api.Path)
                    ? $"/entity-event-listener/{managerName}"
                    : api.Path;
            }

            return api;
        }

        /// <summary>
        /// Применяет настройки менеджера к запросу.
        /// </summary>
        private TQuery ApplyQuerySettings<TQuery>(TQuery query)
            where TQuery : EntitySchemaQuery
        {
            query.MaxReadRowCount = Options.MaxReadRowCount;
            return query;
        }

        #endregion Private Methods
    }
}
