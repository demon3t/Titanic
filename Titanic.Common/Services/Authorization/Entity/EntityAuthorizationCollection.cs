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

        /// <summary>
        /// Добавить или обновить авторизацию пользователя по ключу.
        /// </summary>
        /// <param name="key">Ключ авторизации.</param>
        /// <param name="userConnection">Контекст пользователя.</param>
        public void AddAuthorization(string key, UserConnection userConnection)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentNullException.ThrowIfNull(userConnection);

            _authorizationCollection[key] = (GetUtcNow(), userConnection);
        }

        /// <summary>
        /// Удалить авторизацию по ключу.
        /// </summary>
        /// <param name="key">Ключ авторизации.</param>
        public void RemoveAuthorization(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            _authorizationCollection.TryRemove(key, out _);
        }

        /// <summary>
        /// Проверить авторизацию по ключу и обновить время последнего доступа для активной записи.
        /// </summary>
        /// <param name="key">Ключ авторизации.</param>
        /// <returns>Возвращает true, если ключ авторизации найден и не истёк.</returns>
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

        /// <summary>
        /// Попытаться получить авторизацию из внешнего источника.
        /// </summary>
        /// <param name="key">Ключ авторизации.</param>
        /// <param name="userConnection">Контекст пользователя.</param>
        /// <returns>Возвращает true, если авторизация найдена во внешнем источнике.</returns>
        public virtual bool TryGet(string key, out UserConnection? userConnection)
        {
            userConnection = null;
            return false;
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Получить текущее время в UTC через настроенный провайдер времени.
        /// </summary>
        /// <returns>Текущее время в UTC.</returns>
        private DateTimeOffset GetUtcNow() => _timeProvider.GetUtcNow();

        /// <summary>
        /// Проверить, истекло ли время жизни авторизации.
        /// </summary>
        /// <param name="lastAccessUtc">Время последнего доступа в UTC.</param>
        /// <returns>Возвращает true, если авторизация устарела.</returns>
        private bool IsExpired(DateTimeOffset lastAccessUtc) => GetUtcNow() - lastAccessUtc > _maxLifetime;

        #endregion Private Methods
    }
}
