using System.Reflection;
using Titanic.Common.Session;
using Titanic.Db;
using Titanic.Db.Abstractions;
using Titanic.Entity.Attributes;
using Titanic.Entity.Interfaces;
using Titanic.Entity.Orm;
using Titanic.Entity.Strurture;
using Titanic.Entity.WebApplication.Configuration;

namespace Titanic.Entity
{
    /// <summary>
    /// Статический менеджер Entity ORM: создаёт запросы и хранит реестр Entity ORM менеджеров.
    /// </summary>
    public static class EntityManager
    {
        #region Fields

        private static readonly Dictionary<Type, BaseEntityManager> _managers = new();
        private static readonly object _initLock = new();

        #endregion Fields

        #region Initialization

        /// <summary>
        /// Инициализировать реестр Entity ORM менеджеров из конфигурации.
        /// </summary>
        /// <param name="config"> Конфигурация менеджеров. </param>
        public static void Initialize(EntityManagerConfig config)
        {
            ArgumentNullException.ThrowIfNull(config);

            lock (_initLock)
            {
                _managers.Clear();

                foreach (var managerConfig in config.Managers)
                {
                    RegisterManager(managerConfig);
                }
            }
        }

        /// <summary>
        /// Сбросить реестр Entity ORM менеджеров.
        /// </summary>
        public static void Reset()
        {
            lock (_initLock)
            {
                _managers.Clear();
            }
        }

        /// <summary>
        /// Зарегистрировать Entity ORM менеджер из конфигурации.
        /// </summary>
        /// <param name="config"> Конфигурация менеджера. </param>
        /// <returns> Инициализированный менеджер. </returns>
        public static BaseEntityManager RegisterManager(EntityManagerSettings config)
        {
            ArgumentNullException.ThrowIfNull(config);

            var name = string.IsNullOrWhiteSpace(config.Name)
                ? ResolveManagerName(config.ManagerType, config.DbProviderName)
                : config.Name;

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Entity manager name is empty", nameof(config));
            }

            var providerName = string.IsNullOrWhiteSpace(config.DbProviderName)
                ? name
                : config.DbProviderName;
            var provider = DbManager.GetProvider(providerName);
            var manager = CreateManagerInstance(config.ManagerType);
            manager.Initialize(name, provider, config);
            RegisterManager(manager);
            return manager;
        }

        /// <summary>
        /// Зарегистрировать Entity ORM менеджер по типу и провайдеру DbManager.
        /// </summary>
        /// <typeparam name="TManager"> Тип менеджера. </typeparam>
        /// <param name="dbProviderName"> Имя провайдера в DbManager. </param>
        /// <param name="explicitName"> Явное имя менеджера. </param>
        public static void RegisterManager<TManager>(string dbProviderName, string? explicitName = null)
            where TManager : BaseEntityManager, new()
        {
            if (string.IsNullOrWhiteSpace(dbProviderName))
            {
                throw new ArgumentException("DB provider name is empty", nameof(dbProviderName));
            }

            var name = explicitName ?? ResolveManagerName(typeof(TManager), dbProviderName);
            var manager = BaseEntityManager.BuildManager<TManager>(name, DbManager.GetProvider(dbProviderName));
            RegisterManager(manager);
        }

        /// <summary>
        /// Зарегистрировать готовый Entity ORM менеджер.
        /// </summary>
        /// <param name="manager"> Инициализированный менеджер. </param>
        public static void RegisterManager(BaseEntityManager manager)
        {
            ArgumentNullException.ThrowIfNull(manager);
            if (string.IsNullOrWhiteSpace(manager.Name))
            {
                throw new ArgumentException("Entity manager name is empty", nameof(manager));
            }

            var managerType = manager.GetType();
            if (_managers.ContainsKey(managerType))
            {
                throw new InvalidOperationException(
                    $"Entity manager of type '{managerType.FullName}' is already registered.");
            }

            _managers[managerType] = manager;
        }

        #endregion Initialization

        #region Registered Managers

        /// <summary>
        /// Получить Entity ORM менеджер по типу.
        /// </summary>
        /// <typeparam name="TManager"> Тип менеджера. </typeparam>
        /// <returns> Зарегистрированный менеджер. </returns>
        public static TManager Get<TManager>() where TManager : BaseEntityManager
        {
            return GetManager<TManager>();
        }

        /// <summary>
        /// Получить Entity ORM менеджер по типу.
        /// </summary>
        /// <typeparam name="TManager"> Тип менеджера. </typeparam>
        /// <returns> Зарегистрированный менеджер. </returns>
        public static TManager GetManager<TManager>() where TManager : BaseEntityManager
        {
            return _managers.TryGetValue(typeof(TManager), out var manager)
                ? (TManager)manager
                : throw new KeyNotFoundException(
                    $"Entity manager of type '{typeof(TManager).FullName}' is not registered.");
        }

        /// <summary>
        /// Получить все зарегистрированные Entity ORM менеджеры.
        /// </summary>
        /// <returns> Список менеджеров. </returns>
        public static IReadOnlyCollection<BaseEntityManager> GetManagers()
        {
            return _managers.Values.ToArray();
        }

        #endregion Registered Managers

        #region Select Builders

        public static EntitySelectBuilder<TEntity> Select<TEntity>(UserConnection userConnection)
        {
            return GetSingleManager().Select<TEntity>(userConnection);
        }

        public static EntitySelectBuilder<TEntity> Select<TManager, TEntity>(UserConnection userConnection)
            where TManager : BaseEntityManager
        {
            return GetManager<TManager>().Select<TEntity>(userConnection);
        }

        public static EntitySelectBuilder<TEntity> Select<TEntity>(BaseDbProvider provider, UserConnection userConnection)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(userConnection);
            return new EntitySelectBuilder<TEntity>(provider, userConnection);
        }

        public static EntitySelectBuilder Select(Type entityType, BaseDbProvider provider, UserConnection userConnection)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(userConnection);
            return new EntitySelectBuilder(provider, entityType, userConnection);
        }

        public static EntitySelectBuilder Select(string tableName, BaseDbProvider provider, UserConnection userConnection)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(userConnection);
            return new EntitySelectBuilder(provider, tableName, userConnection);
        }

        public static EntitySelectBuilder<TEntity> Select<TEntity>(BaseDatabase database, UserConnection userConnection)
        {
            ArgumentNullException.ThrowIfNull(database);
            return Select<TEntity>(GetProvider(database), userConnection);
        }

        public static EntitySelectBuilder Select(Type entityType, BaseDatabase database, UserConnection userConnection)
        {
            ArgumentNullException.ThrowIfNull(database);
            return Select(entityType, GetProvider(database), userConnection);
        }

        public static EntitySelectBuilder Select(string tableName, BaseDatabase database, UserConnection userConnection)
        {
            ArgumentNullException.ThrowIfNull(database);
            return Select(tableName, GetProvider(database), userConnection);
        }

        #endregion Select Builders

        #region Entity Schema Queries

        public static EntitySchemaQuery<TEntity> Query<TEntity>(UserConnection userConnection)
        {
            return GetSingleManager().Query<TEntity>(userConnection);
        }

        public static EntitySchemaQuery<TEntity> Query<TManager, TEntity>(UserConnection userConnection)
            where TManager : BaseEntityManager
        {
            return GetManager<TManager>().Query<TEntity>(userConnection);
        }

        public static EntitySchemaQuery<TEntity> Query<TEntity>(BaseDbProvider provider, UserConnection userConnection)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(userConnection);
            return new EntitySchemaQuery<TEntity>(provider, userConnection);
        }

        public static EntitySchemaQuery Query(Type entityType, BaseDbProvider provider, UserConnection userConnection)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(userConnection);
            return new EntitySchemaQuery(provider, entityType, userConnection);
        }

        public static EntitySchemaQuery Query(string tableName, BaseDbProvider provider, UserConnection userConnection)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(userConnection);
            return new EntitySchemaQuery(provider, tableName, userConnection);
        }

        public static EntitySchemaQuery<TEntity> Query<TEntity>(BaseDatabase database, UserConnection userConnection)
        {
            ArgumentNullException.ThrowIfNull(database);
            return Query<TEntity>(GetProvider(database), userConnection);
        }

        public static EntitySchemaQuery Query(Type entityType, BaseDatabase database, UserConnection userConnection)
        {
            ArgumentNullException.ThrowIfNull(database);
            return Query(entityType, GetProvider(database), userConnection);
        }

        public static EntitySchemaQuery Query(string tableName, BaseDatabase database, UserConnection userConnection)
        {
            ArgumentNullException.ThrowIfNull(database);
            return Query(tableName, GetProvider(database), userConnection);
        }

        #endregion Entity Schema Queries

        #region Entity CRUD

        public static Orm.Entity Create<TEntity>(UserConnection userConnection)
        {
            return GetSingleManager().Create<TEntity>(userConnection);
        }

        public static Orm.Entity Create<TManager, TEntity>(UserConnection userConnection)
            where TManager : BaseEntityManager
        {
            return GetManager<TManager>().Create<TEntity>(userConnection);
        }

        public static Orm.Entity Create<TEntity>(BaseDbProvider provider, UserConnection userConnection)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(userConnection);
            return Create(Structure.GetEntityStructure<TEntity>(), provider, userConnection);
        }

        public static Orm.Entity Create(Type entityType, BaseDbProvider provider, UserConnection userConnection)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(userConnection);
            return Create(Structure.GetEntityStructure(entityType), provider, userConnection);
        }

        public static Orm.Entity Create(string tableName, BaseDbProvider provider, UserConnection userConnection)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(userConnection);
            return Create(Structure.GetEntityStructure(tableName), provider, userConnection);
        }

        public static Orm.Entity Create<TEntity>(BaseDatabase database, UserConnection userConnection)
        {
            ArgumentNullException.ThrowIfNull(database);
            return Create<TEntity>(GetProvider(database), userConnection);
        }

        public static Orm.Entity Create(Type entityType, BaseDatabase database, UserConnection userConnection)
        {
            ArgumentNullException.ThrowIfNull(database);
            return Create(entityType, GetProvider(database), userConnection);
        }

        public static Orm.Entity Create(string tableName, BaseDatabase database, UserConnection userConnection)
        {
            ArgumentNullException.ThrowIfNull(database);
            return Create(tableName, GetProvider(database), userConnection);
        }

        #endregion Entity CRUD

        #region Private Methods

        private static Orm.Entity Create(EntityStructure structure, BaseDbProvider provider, UserConnection userConnection)
        {
            var pathToAlias = structure.ColumnsStructure
                .SelectMany(column => new[]
                {
                    new KeyValuePair<string, string>(column.PropertyName, column.PropertyName),
                    new KeyValuePair<string, string>(column.ColumnName, column.PropertyName)
                })
                .GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.First().Value, StringComparer.OrdinalIgnoreCase);

            var aliasToColumn = structure.ColumnsStructure
                .SelectMany(column => new[]
                {
                    new KeyValuePair<string, ColumnStructure>(column.PropertyName, column),
                    new KeyValuePair<string, ColumnStructure>(column.ColumnName, column)
                })
                .GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.First().Value, StringComparer.OrdinalIgnoreCase);

            return new Orm.Entity(
                new Dictionary<string, Orm.ColumnValue>(StringComparer.OrdinalIgnoreCase),
                pathToAlias,
                aliasToColumn,
                structure,
                provider,
                userConnection);
        }

        private static BaseEntityManager GetSingleManager()
        {
            return _managers.Count switch
            {
                0 => throw new InvalidOperationException("Entity manager is not configured."),
                1 => _managers.Values.Single(),
                _ => throw new InvalidOperationException(
                    "More than one Entity manager is registered. Use GetManager<TManager>() or typed query methods.")
            };
        }

        private static BaseDbProvider GetProvider(BaseDatabase database)
        {
            return database.Select().Provider
                ?? throw new InvalidOperationException("Database provider is not initialized.");
        }

        private static BaseEntityManager CreateManagerInstance(string? managerTypeName)
        {
            if (string.IsNullOrWhiteSpace(managerTypeName))
            {
                return new EntityDbManager();
            }

            var managerType = Type.GetType(managerTypeName)
                ?? throw new InvalidOperationException($"Entity manager type '{managerTypeName}' not found.");
            if (!typeof(BaseEntityManager).IsAssignableFrom(managerType))
            {
                throw new InvalidOperationException(
                    $"Entity manager type '{managerTypeName}' must inherit BaseEntityManager.");
            }

            return Activator.CreateInstance(managerType) is BaseEntityManager manager
                ? manager
                : throw new InvalidOperationException($"Cannot create Entity manager '{managerTypeName}'.");
        }

        private static string ResolveManagerName(string? managerTypeName, string dbProviderName)
        {
            if (!string.IsNullOrWhiteSpace(managerTypeName))
            {
                var managerType = Type.GetType(managerTypeName);
                if (managerType != null)
                {
                    return ResolveManagerName(managerType, dbProviderName);
                }
            }

            return dbProviderName;
        }

        private static string ResolveManagerName(Type managerType, string dbProviderName)
        {
            var attr = managerType.GetCustomAttribute<EntityManagerConnectionAttribute>();
            if (attr != null)
            {
                return attr.Name;
            }

            return string.IsNullOrWhiteSpace(dbProviderName)
                ? managerType.Name
                : dbProviderName;
        }

        #endregion Private Methods
    }
}
