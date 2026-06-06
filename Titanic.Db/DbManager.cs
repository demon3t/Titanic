using System.Reflection;
using Titanic.Db.Abstractions;
using Titanic.Db.Attributes;
using Titanic.Db.Configuration;
using Titanic.Db.Enums;
using Titanic.Db.PosgreSql;

namespace Titanic.Db
{
    /// <summary>
    /// Менеджер провайдеров БД и обёрток <see cref="BaseDatabase"/>.
    /// Хранит единый экземпляр каждой обёртки в статических словарях — singleton на уровне AppDomain.
    /// Инициализируется через <see cref="Initialize(DbConfig)"/> (один раз),
    /// после чего обёртки доступны через <see cref="Get{TDatabase}()"/>.
    /// </summary>
    public static class DbManager
    {
        private static readonly Dictionary<string, BaseDbProvider> _providers =
            new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, BaseDatabase> _databases =
            new(StringComparer.OrdinalIgnoreCase);
        private static string? _defaultProviderName;
        private static readonly object _initLock = new();

        /// <summary>
        /// Инициализировать менеджер из конфигурации.
        /// Регистрирует все провайдеры и обёртки, описанные в <paramref name="config"/>.
        /// Повторный вызов пересоздаёт состояние (полезно для тестов).
        /// </summary>
        public static void Initialize(DbConfig config)
        {
            ArgumentNullException.ThrowIfNull(config);

            lock (_initLock)
            {
                _providers.Clear();
                _databases.Clear();
                _defaultProviderName = null;

                foreach (var providerConfig in config.Providers)
                {
                    RegisterProvider(providerConfig);
                }

                _defaultProviderName = string.IsNullOrWhiteSpace(config.DefaultProviderName)
                    ? _providers.Keys.FirstOrDefault()
                    : config.DefaultProviderName;
            }
        }

        /// <summary>
        /// Сбросить состояния (полезно для тестов).
        /// </summary>
        public static void Reset()
        {
            lock (_initLock)
            {
                _providers.Clear();
                _databases.Clear();
                _defaultProviderName = null;
            }
        }

        /// <summary>
        /// Зарегистрировать провайдер из конфигурации.
        /// Если в <see cref="DbProviderConfig.Types"/> задан <see cref="ProviderTypeConfig.DatabaseType"/>,
        /// то обёртка поднимается через reflection и регистрируется под именем провайдера.
        /// </summary>
        public static DbProviderConfig RegisterProvider(DbProviderConfig config)
        {
            ArgumentNullException.ThrowIfNull(config);

            if (string.IsNullOrWhiteSpace(config.Name))
            {
                throw new ArgumentException("Provider name is empty", nameof(config));
            }

            var provider = CreateProvider(config);
            RegisterProvider(config.Name, provider);

            // Если в конфиге задан тип обёртки — создаём и регистрируем её под именем провайдера.
            if (config.Types is not null && !string.IsNullOrWhiteSpace(config.Types.DatabaseType))
            {
                var database = ProviderReflectionFactory.CreateDatabase(config.Types.DatabaseType, config.Name, provider);
                RegisterDatabase(database);
            }

            return config;
        }

        /// <summary>
        /// Зарегистрировать уже созданный провайдер.
        /// </summary>
        public static void RegisterProvider(string name, BaseDbProvider provider)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Provider name is empty", nameof(name));
            }

            ArgumentNullException.ThrowIfNull(provider);

            _providers[name] = provider;
            _defaultProviderName ??= name;
        }

        /// <summary>
        /// Зарегистрировать пользовательскую обёртку подключения <see cref="BaseDatabase"/>.
        /// Имя берётся из атрибута <see cref="DatabaseConnectionAttribute"/> на классе,
        /// либо передаётся явно.
        /// </summary>
        /// <typeparam name="TDatabase">Тип обёртки.</typeparam>
        /// <param name="providerName">Имя провайдера в менеджере.</param>
        /// <param name="explicitName">Явное имя подключения (если null — берётся из атрибута).</param>
        public static void RegisterDatabase<TDatabase>(string providerName, string? explicitName = null)
            where TDatabase : BaseDatabase, new()
        {
            var database = new TDatabase();
            var name = explicitName ?? ResolveConnectionName(typeof(TDatabase), providerName);
            database.Initialize(name, GetProvider(providerName));
            RegisterDatabase(database);
        }

        /// <summary>
        /// Зарегистрировать уже созданный инстанс <see cref="BaseDatabase"/> по его имени.
        /// </summary>
        public static void RegisterDatabase(BaseDatabase database)
        {
            ArgumentNullException.ThrowIfNull(database);
            if (string.IsNullOrWhiteSpace(database.Name))
            {
                throw new ArgumentException("Database name is empty", nameof(database));
            }

            _databases[database.Name] = database;
        }

        /// <summary>
        /// Получить инстанс пользовательской обёртки подключения по типу.
        /// Ищет в зарегистрированных обёртках по runtime-типу.
        /// Обёртка должна быть зарегистрирована заранее:
        /// через <see cref="Initialize(DbConfig)"/> (конфиг с <c>Types.DatabaseType</c>),
        /// либо явно через <see cref="RegisterDatabase{TDatabase}(string, string?)"/>.
        /// </summary>
        /// <typeparam name="TDatabase">Тип обёртки (наследник <see cref="BaseDatabase"/>).</typeparam>
        public static TDatabase Get<TDatabase>() where TDatabase : BaseDatabase
        {
            foreach (var db in _databases.Values)
            {
                if (db is TDatabase typed)
                {
                    return typed;
                }
            }

            throw new KeyNotFoundException(
                $"Database of type '{typeof(TDatabase).FullName}' is not registered. " +
                "Add it to the configuration (Types.DatabaseType) or call RegisterDatabase<T>() explicitly.");
        }

        /// <summary>
        /// Получить подключение по имени.
        /// </summary>
        public static BaseDatabase GetDatabase(string name)
        {
            return _databases.TryGetValue(name, out var db)
                ? db
                : throw new KeyNotFoundException($"Database '{name}' is not registered");
        }

        /// <summary>
        /// Получить провайдер по имени или провайдер по умолчанию.
        /// </summary>
        public static BaseDbProvider GetProvider(string? name = null)
        {
            var providerName = string.IsNullOrWhiteSpace(name) ? _defaultProviderName : name;
            if (string.IsNullOrWhiteSpace(providerName))
            {
                throw new InvalidOperationException("Default DB provider is not configured");
            }

            return _providers.TryGetValue(providerName, out var provider)
                ? provider
                : throw new KeyNotFoundException($"DB provider '{providerName}' is not configured");
        }

        /// <summary>
        /// Создать экземпляр провайдера по конфигурации.
        /// Приоритет: <see cref="ProviderTypeConfig"/> через reflection,
        /// затем встроенная фабрика по <see cref="Enums.DatabaseType"/>.
        /// </summary>
        private static BaseDbProvider CreateProvider(DbProviderConfig config)
        {
            if (config.Types is not null)
            {
                return ProviderReflectionFactory.CreateProvider(config.Types, config.ConnectionString);
            }

            if (config.DatabaseType.HasValue)
            {
                return config.DatabaseType.Value switch
                {
                    DatabaseType.Postgres => new PostgresProvider(config.ConnectionString),
                    _ => throw new NotSupportedException($"Database type {config.DatabaseType} is not supported")
                };
            }

            throw new InvalidOperationException(
                "DbProviderConfig requires either 'Types' (reflection) or 'DatabaseType' (built-in).");
        }

        /// <summary>
        /// Получить имя подключения из атрибута <see cref="DatabaseConnectionAttribute"/>,
        /// либо сгенерировать на основе имени провайдера и имени типа.
        /// </summary>
        private static string ResolveConnectionName(Type databaseType, string providerName)
        {
            var attr = databaseType.GetCustomAttribute<DatabaseConnectionAttribute>();
            if (attr != null) return attr.Name;

            return $"{providerName}:{databaseType.Name}";
        }
    }
}
