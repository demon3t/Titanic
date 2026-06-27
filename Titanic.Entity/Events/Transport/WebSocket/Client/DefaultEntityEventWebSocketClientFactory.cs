using System.Collections.Concurrent;

namespace Titanic.Entity.Events
{
    /// <summary>
    /// Стандартная фабрика persistent WebSocket-клиентов событийного слоя.
    /// </summary>
    public sealed class DefaultEntityEventWebSocketClientFactory : IEntityEventWebSocketClientFactory, IDisposable
    {
        #region Fields

        /// <summary>
        /// Кэш persistent WebSocket-клиентов по абсолютному URI listener API.
        /// </summary>
        private readonly ConcurrentDictionary<string, EntityEventWebSocketClient> _clients = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Признак того, что фабрика уже освобождена и больше не должна возвращать клиентов.
        /// </summary>
        private bool _disposed;

        #endregion Fields

        #region Members

        /// <inheritdoc />
        public EntityEventWebSocketClient CreateClient(Uri listenerUri)
        {
            ArgumentNullException.ThrowIfNull(listenerUri);

            ThrowIfDisposed();
            return _clients.GetOrAdd(listenerUri.AbsoluteUri, _ => new EntityEventWebSocketClient(listenerUri));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            foreach (var client in _clients.Values)
            {
                client.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }

            _clients.Clear();
            _disposed = true;
        }

        /// <summary>
        /// Выбрасывает ошибку, если фабрика уже освобождена контейнером.
        /// </summary>
        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }

        #endregion Members
    }
}
