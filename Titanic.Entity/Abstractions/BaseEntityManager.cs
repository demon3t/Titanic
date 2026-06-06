using Titanic.Common.Session;
using Titanic.Db.Abstractions;
using Titanic.Entity.Orm;
using Titanic.Entity.Strurture;
using Titanic.Entity.WebApplication.Configuration;

namespace Titanic.Entity.Interfaces
{
    /// <summary>
    /// Базовая обертка Entity ORM над провайдером БД.
    /// </summary>
    public abstract class BaseEntityManager
    {
        #region Properties

        /// <summary>
        /// Имя Entity ORM менеджера в конфигурации.
        /// Реестр менеджеров использует runtime-тип менеджера, а не это имя.
        /// </summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// Провайдер БД, через который Entity ORM строит и выполняет запросы.
        /// </summary>
        public BaseDbProvider Provider { get; private set; } = null!;

        /// <summary>
        /// Настройки публикации HTTP API для менеджера.
        /// </summary>
        public EntityManagerApiSettings Api { get; private set; } = new();

        /// <summary>
        /// Runtime-опции Entity ORM менеджера из конфигурации.
        /// </summary>
        public EntityManagerOptions Options { get; private set; } = new();

        /// <summary>
        /// Признак проверки структуры БД на наличие таблиц и колонок Entity-моделей при инициализации.
        /// </summary>
        public bool ValidateDatabaseSchemaOnCompile { get; private set; }

        #endregion Properties

        #region Initialization

        /// <summary>
        /// Инициализировать менеджер готовым провайдером БД.
        /// </summary>
        /// <param name="name"> Имя менеджера из конфигурации. </param>
        /// <param name="provider"> Провайдер БД. </param>
        /// <param name="settings"> Настройки менеджера. </param>
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
            ValidateDatabaseSchemaOnCompile = settings?.ValidateDatabaseSchemaOnCompile ?? false;

            if (ValidateDatabaseSchemaOnCompile)
            {
                EntitySchemaValidator.Validate(provider);
            }
        }

        /// <summary>
        /// Собрать менеджер из готового провайдера БД.
        /// </summary>
        /// <typeparam name="TManager"> Тип менеджера. </typeparam>
        /// <param name="name"> Имя менеджера. </param>
        /// <param name="provider"> Провайдер БД. </param>
        /// <param name="settings"> Настройки менеджера. </param>
        /// <returns> Инициализированный менеджер. </returns>
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
        /// Создать Entity Schema Query по CLR-модели сущности.
        /// </summary>
        /// <typeparam name="TEntity"> Тип CLR-модели сущности. </typeparam>
        /// <param name="userConnection"> Контекст пользователя. </param>
        /// <returns> Entity Schema Query. </returns>
        public EntitySchemaQuery<TEntity> Query<TEntity>(UserConnection userConnection)
        {
            return ApplyQuerySettings(EntityManager.Query<TEntity>(Provider, userConnection));
        }

        /// <summary>
        /// Создать Entity Schema Query по CLR-типу сущности.
        /// </summary>
        /// <param name="entityType"> CLR-тип сущности. </param>
        /// <param name="userConnection"> Контекст пользователя. </param>
        /// <returns> Entity Schema Query. </returns>
        public EntitySchemaQuery Query(Type entityType, UserConnection userConnection)
        {
            return ApplyQuerySettings(EntityManager.Query(entityType, Provider, userConnection));
        }

        /// <summary>
        /// Создать Entity Schema Query по имени таблицы.
        /// </summary>
        /// <param name="tableName"> Имя таблицы. </param>
        /// <param name="userConnection"> Контекст пользователя. </param>
        /// <returns> Entity Schema Query. </returns>
        public EntitySchemaQuery Query(string tableName, UserConnection userConnection)
        {
            return ApplyQuerySettings(EntityManager.Query(tableName, Provider, userConnection));
        }

        #endregion Entity Queries

        #region Entity Select Builders

        /// <summary>
        /// Создать ORM SELECT builder по CLR-модели сущности.
        /// </summary>
        /// <typeparam name="TEntity"> Тип CLR-модели сущности. </typeparam>
        /// <param name="userConnection"> Контекст пользователя. </param>
        /// <returns> ORM SELECT builder. </returns>
        public EntitySelectBuilder<TEntity> Select<TEntity>(UserConnection userConnection)
        {
            return EntityManager.Select<TEntity>(Provider, userConnection);
        }

        /// <summary>
        /// Создать ORM SELECT builder по CLR-типу сущности.
        /// </summary>
        /// <param name="entityType"> CLR-тип сущности. </param>
        /// <param name="userConnection"> Контекст пользователя. </param>
        /// <returns> ORM SELECT builder. </returns>
        public EntitySelectBuilder Select(Type entityType, UserConnection userConnection)
        {
            return EntityManager.Select(entityType, Provider, userConnection);
        }

        /// <summary>
        /// Создать ORM SELECT builder по имени таблицы.
        /// </summary>
        /// <param name="tableName"> Имя таблицы. </param>
        /// <param name="userConnection"> Контекст пользователя. </param>
        /// <returns> ORM SELECT builder. </returns>
        public EntitySelectBuilder Select(string tableName, UserConnection userConnection)
        {
            return EntityManager.Select(tableName, Provider, userConnection);
        }

        #endregion Entity Select Builders

        #region Entity CRUD

        /// <summary>
        /// Создать пустую сущность по CLR-модели.
        /// </summary>
        /// <typeparam name="TEntity"> Тип CLR-модели сущности. </typeparam>
        /// <param name="userConnection"> Контекст пользователя. </param>
        /// <returns> Новая ORM-сущность. </returns>
        public Orm.Entity Create<TEntity>(UserConnection userConnection)
        {
            return EntityManager.Create<TEntity>(Provider, userConnection);
        }

        /// <summary>
        /// Создать пустую сущность по CLR-типу.
        /// </summary>
        /// <param name="entityType"> CLR-тип сущности. </param>
        /// <param name="userConnection"> Контекст пользователя. </param>
        /// <returns> Новая ORM-сущность. </returns>
        public Orm.Entity Create(Type entityType, UserConnection userConnection)
        {
            return EntityManager.Create(entityType, Provider, userConnection);
        }

        /// <summary>
        /// Создать пустую сущность по имени таблицы.
        /// </summary>
        /// <param name="tableName"> Имя таблицы. </param>
        /// <param name="userConnection"> Контекст пользователя. </param>
        /// <returns> Новая ORM-сущность. </returns>
        public Orm.Entity Create(string tableName, UserConnection userConnection)
        {
            return EntityManager.Create(tableName, Provider, userConnection);
        }

        #endregion Entity CRUD

        #region Private Methods

        /// <summary>
        /// Нормализовать настройки API менеджера.
        /// </summary>
        /// <param name="settings"> Настройки из конфигурации. </param>
        /// <returns> Настройки API с заполненными значениями по умолчанию. </returns>
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
        /// Применить настройки менеджера к EntitySchemaQuery.
        /// </summary>
        /// <typeparam name="TQuery"> Тип запроса. </typeparam>
        /// <param name="query"> Запрос сущностей. </param>
        /// <returns> Запрос с примененными настройками менеджера. </returns>
        private TQuery ApplyQuerySettings<TQuery>(TQuery query)
            where TQuery : EntitySchemaQuery
        {
            query.MaxReadRowCount = Options.MaxReadRowCount;
            return query;
        }

        #endregion Private Methods
    }
}
