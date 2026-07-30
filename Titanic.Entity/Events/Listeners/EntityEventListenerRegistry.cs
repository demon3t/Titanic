using System.Reflection;
using Titanic.Common.Services.Factory;
using Titanic.Entity.Attributes;

namespace Titanic.Entity.Events
{
    /// <summary>
    /// Реестр типов обработчиков событий Entity ORM.
    /// </summary>
    internal static class EntityEventListenerRegistry
    {
        #region Members

        private static readonly object SyncRoot = new();
        private static readonly Dictionary<string, List<Type>> ListenersByEntityName =
            new(StringComparer.OrdinalIgnoreCase);
        private static bool _initialized;

        /// <summary>
        /// Инициализирует новый экземпляр RegisterListeners.
        /// </summary>
        internal static void RegisterListeners()
        {
            EnsureInitialized();

            lock (SyncRoot)
            {
                foreach (var entityListeners in ListenersByEntityName)
                {
                    foreach (var listenerType in entityListeners.Value)
                    {
                        ClassFactory.Bind(typeof(BaseEntityEventListener), listenerType, entityListeners.Key);
                    }
                }
            }
        }

        /// <summary>
        /// Инициализирует новый экземпляр GetListeners.
        /// </summary>
        internal static IReadOnlyCollection<BaseEntityEventListener> GetListeners(string entityName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(entityName);

            EnsureInitialized();

            return ClassFactory
                .GetAll<BaseEntityEventListener>(
                    entityName,
                    new ConstructorArgument("entityName", entityName))
                .ToArray();
        }

        /// <summary>
        /// Инициализирует новый экземпляр Reset.
        /// </summary>
        internal static void Reset()
        {
            lock (SyncRoot)
            {
                ListenersByEntityName.Clear();
                _initialized = false;
            }
        }

        /// <summary>
        /// Инициализирует новый экземпляр EnsureInitialized.
        /// </summary>
        private static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            lock (SyncRoot)
            {
                if (_initialized)
                {
                    return;
                }

                ListenersByEntityName.Clear();

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(x => !x.IsDynamic))
                {
                    RegisterAssembly(assembly);
                }

                _initialized = true;
            }
        }

        /// <summary>
        /// Инициализирует новый экземпляр RegisterAssembly.
        /// </summary>
        private static void RegisterAssembly(Assembly assembly)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                types = exception.Types.Where(type => type != null).Cast<Type>().ToArray();
            }

            foreach (var type in types)
            {
                if (type.IsAbstract || !typeof(BaseEntityEventListener).IsAssignableFrom(type))
                {
                    continue;
                }

                var attribute = type.GetCustomAttribute<EntityEventListenerAttribute>();
                if (attribute == null)
                {
                    continue;
                }

                if (!ListenersByEntityName.TryGetValue(attribute.EntityName, out var listeners))
                {
                    listeners = new List<Type>();
                    ListenersByEntityName[attribute.EntityName] = listeners;
                }

                if (!listeners.Contains(type))
                {
                    listeners.Add(type);
                }
            }
        }

        #endregion Members
    }
}
