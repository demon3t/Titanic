using System.Collections.Concurrent;
using Titanic.Common.Services.Authorization.Interfaces;
using Titanic.Common.Session;

namespace Titanic.Common.Services.Authorization.Entity
{
    /// <summary>
    /// In-memory коллекция авторизаций для запросов к сущностям.
    /// </summary>
    public class EntityAuthorizationCollection : IAuthorizationCollection
    {
        #region Fields

        /// <summary>
        /// Коллекция авторизаций.
        /// </summary>
        private readonly ConcurrentDictionary<string, (DateTimeOffset LastAccessUtc, UserConnection Connection)> _authorizationCollection;

        /// <summary>
        /// Провайдер времени.
        /// </summary>
        private readonly TimeProvider _timeProvider;

        /// <summary>
        /// Время жизни авторизации.
        /// </summary>
        private readonly TimeSpan _maxLifetime;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Конструктор коллекции авторизаций.
        /// </summary>
        /// <param name="timeProvider">Провайдер времени.</param>
        /// <param name="maxLifetime">Время жизни авторизации.</param>
        public EntityAuthorizationCollection(TimeProvider? timeProvider = null, TimeSpan? maxLifetime = null)
        {
            _timeProvider = timeProvider ?? TimeProvider.System;
            _maxLifetime = maxLifetime ?? TimeSpan.FromDays(1);
            _authorizationCollection = new ConcurrentDictionary<string, (DateTimeOffset LastAccessUtc, UserConnection Connection)>(StringComparer.Ordinal);
        }

        #endregion Constructors

        #region Public Methods

        /// <inheritdoc />
        public void AddAuthorization(string key, UserConnection userConnection)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentNullException.ThrowIfNull(userConnection);

            _authorizationCollection[key] = (GetUtcNow(), userConnection);
        }

        /// <inheritdoc />
        public void RemoveAuthorization(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            _authorizationCollection.TryRemove(key, out _);
        }

        /// <inheritdoc />
        public bool CheckAuthorization(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            if (_authorizationCollection.TryGetValue(key, out var value))
            {
                if (!IsExpired(value.LastAccessUtc))
                {
                    _authorizationCollection[key] = (GetUtcNow(), value.Connection);
                    return true;
                }

                _authorizationCollection.TryRemove(key, out _);
            }

            if (TryGet(key, out var userConnection) && userConnection is not null)
            {
                AddAuthorization(key, userConnection);
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public bool TryGet(string key, out UserConnection? userConnection)
        {
            userConnection = null;
            return false;
        }

        #endregion Public Methods

        #region Private Methods

        private DateTimeOffset GetUtcNow() => _timeProvider.GetUtcNow();

        private bool IsExpired(DateTimeOffset lastAccessUtc) => GetUtcNow() - lastAccessUtc > _maxLifetime;

        #endregion Private Methods
    }
}
