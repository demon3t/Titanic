using System.Reflection;
using Titanic.Db.Abstractions;

namespace Titanic.Db.Configuration
{
    /// <summary>
    /// Фабрика провайдеров/движков/обёрток БД на базе reflection.
    /// Позволяет поднять нужные классы по полному имени типа
    /// без явной compile-time зависимости на конкретную сборку.
    /// </summary>
    public static class ProviderReflectionFactory
    {
        /// <summary>
        /// Создать экземпляр <see cref="BaseDbProvider"/> по описанию типов.
        /// </summary>
        /// <param name="types">Описание типов провайдера и движка.</param>
        /// <param name="connectionString">Строка подключения.</param>
        public static BaseDbProvider CreateProvider(ProviderTypeConfig types, string connectionString)
        {
            ArgumentNullException.ThrowIfNull(types);

            var engine = CreateEngine(types.EngineType);
            return CreateProvider(types.ProviderType, engine, connectionString);
        }

        /// <summary>
        /// Создать экземпляр <see cref="BaseDbProvider"/> по имени типа и готовому движку.
        /// </summary>
        public static BaseDbProvider CreateProvider(string providerTypeName, BaseDbEngine engine, string connectionString)
        {
            if (string.IsNullOrWhiteSpace(providerTypeName))
                throw new ArgumentException("Provider type name is empty", nameof(providerTypeName));
            if (engine == null)
                throw new ArgumentNullException(nameof(engine));

            var providerType = ResolveType(providerTypeName, typeof(BaseDbProvider));
            return InvokeCtor<BaseDbProvider>(providerType, new object?[] { connectionString, engine });
        }

        /// <summary>
        /// Создать экземпляр <see cref="BaseDbEngine"/> по имени типа.
        /// </summary>
        public static BaseDbEngine CreateEngine(string engineTypeName)
        {
            if (string.IsNullOrWhiteSpace(engineTypeName))
                throw new ArgumentException("Engine type name is empty", nameof(engineTypeName));

            var engineType = ResolveType(engineTypeName, typeof(BaseDbEngine));
            return (BaseDbEngine)Activator.CreateInstance(engineType, nonPublic: true)!;
        }

        /// <summary>
        /// Создать экземпляр <see cref="BaseDatabase"/> по имени типа и привязать к провайдеру.
        /// </summary>
        public static BaseDatabase CreateDatabase(string databaseTypeName, string connectionName, BaseDbProvider provider)
        {
            if (string.IsNullOrWhiteSpace(databaseTypeName))
                throw new ArgumentException("Database type name is empty", nameof(databaseTypeName));
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            var databaseType = ResolveType(databaseTypeName, typeof(BaseDatabase));
            var instance = (BaseDatabase)Activator.CreateInstance(databaseType, nonPublic: true)!;
            instance.Initialize(connectionName, provider);
            return instance;
        }

        private static Type ResolveType(string typeName, Type expectedBaseType)
        {
            var type = Type.GetType(typeName, throwOnError: false)
                ?? throw new InvalidOperationException(
                    $"Type '{typeName}' was not found. " +
                    $"Use assembly-qualified name (e.g. 'MyNs.MyProvider, MyAssembly').");

            if (!expectedBaseType.IsAssignableFrom(type))
            {
                throw new InvalidOperationException(
                    $"Type '{typeName}' must inherit from {expectedBaseType.FullName}");
            }

            return type;
        }

        private static T InvokeCtor<T>(Type type, object?[] args)
        {
            try
            {
                return (T)Activator.CreateInstance(type, args)!;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        }
    }
}
