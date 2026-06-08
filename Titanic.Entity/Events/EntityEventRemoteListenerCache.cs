using Titanic.Entity.Interfaces;

namespace Titanic.Entity.Events
{
    /// <summary>
    /// Кэш экземпляров listener-ов для обработки одной сущности во внешнем событийном API.
    /// </summary>
    internal static class EntityEventRemoteListenerCache
    {
        #region Members

        private static readonly object SyncRoot = new();
        private static readonly Dictionary<CacheKey, CacheEntry> Entries = new();

        /// <summary>
        /// Возвращает cached listener-ы для dispatch-запроса или создаёт новые.
        /// </summary>
        /// <param name="manager">Менеджер Entity ORM.</param>
        /// <param name="request">Dispatch-запрос события.</param>
        /// <returns>Экземпляры listener-ов.</returns>
        internal static IReadOnlyCollection<BaseEntityEventListener> GetListeners(
            BaseEntityManager manager,
            EntityEventDispatchRequest request)
        {
            ArgumentNullException.ThrowIfNull(manager);
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.DispatchId))
            {
                return EntityEventListenerRegistry.GetListeners(request.TableName);
            }

            var key = new CacheKey(manager.Name, request.TableName, request.DispatchId);
            var timeout = NormalizeTimeout(manager.EventListenerApi.ListenerInstanceIdleTimeout);
            var now = DateTimeOffset.UtcNow;

            lock (SyncRoot)
            {
                RemoveExpired(now);

                if (Entries.TryGetValue(key, out var cached))
                {
                    cached.ExpiresAtUtc = now.Add(timeout);
                    return cached.Listeners;
                }

                var listeners = EntityEventListenerRegistry.GetListeners(request.TableName);
                if (listeners.Count > 0)
                {
                    Entries[key] = new CacheEntry(listeners, now.Add(timeout));
                }

                return listeners;
            }
        }

        /// <summary>
        /// Удаляет cached listener-ы для dispatch-запроса.
        /// </summary>
        /// <param name="manager">Менеджер Entity ORM.</param>
        /// <param name="request">Dispatch-запрос события.</param>
        internal static void Release(BaseEntityManager manager, EntityEventDispatchRequest request)
        {
            ArgumentNullException.ThrowIfNull(manager);
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.DispatchId))
            {
                return;
            }

            var key = new CacheKey(manager.Name, request.TableName, request.DispatchId);
            lock (SyncRoot)
            {
                Entries.Remove(key);
            }
        }

        /// <summary>
        /// Сбрасывает все cached listener-ы.
        /// </summary>
        internal static void Reset()
        {
            lock (SyncRoot)
            {
                Entries.Clear();
            }
        }

        /// <summary>
        /// Удаляет listener-ы, которые не получали запросы дольше TTL.
        /// </summary>
        /// <param name="now">Текущее время.</param>
        private static void RemoveExpired(DateTimeOffset now)
        {
            foreach (var key in Entries
                         .Where(entry => entry.Value.ExpiresAtUtc <= now)
                         .Select(entry => entry.Key)
                         .ToArray())
            {
                Entries.Remove(key);
            }
        }

        /// <summary>
        /// Нормализует TTL cached listener-а.
        /// </summary>
        /// <param name="timeout">Настроенный TTL.</param>
        /// <returns>Положительный TTL.</returns>
        private static TimeSpan NormalizeTimeout(TimeSpan timeout)
        {
            return timeout > TimeSpan.Zero
                ? timeout
                : TimeSpan.FromMinutes(5);
        }

        private readonly record struct CacheKey(string ManagerName, string TableName, string DispatchId);

        private sealed class CacheEntry(
            IReadOnlyCollection<BaseEntityEventListener> listeners,
            DateTimeOffset expiresAtUtc)
        {
            public IReadOnlyCollection<BaseEntityEventListener> Listeners { get; } = listeners;

            public DateTimeOffset ExpiresAtUtc { get; set; } = expiresAtUtc;
        }

        #endregion Members
    }
}
