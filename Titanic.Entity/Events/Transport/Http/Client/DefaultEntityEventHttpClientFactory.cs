using System.Collections.Concurrent;

namespace Titanic.Entity.Events
{
    /// <summary>
    /// Стандартная фабрика HTTP-клиентов событийного слоя.
    /// </summary>
    public sealed class DefaultEntityEventHttpClientFactory : IEntityEventHttpClientFactory, IDisposable
    {
        #region Fields

        /// <summary>
        /// Кэш HTTP-клиентов по authority listener-ов, чтобы не создавать новый HttpClient на каждый dispatch.
        /// </summary>
        private readonly ConcurrentDictionary<string, HttpClient> _clients = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Признак того, что фабрика уже освобождена и больше не должна выдавать клиентов.
        /// </summary>
        private bool _disposed;

        #endregion Fields

        #region Members

        /// <inheritdoc />
        public HttpClient CreateClient(Uri listenerUri)
        {
            ArgumentNullException.ThrowIfNull(listenerUri);

            ThrowIfDisposed();

            var authority = listenerUri.GetLeftPart(UriPartial.Authority);
            return _clients.GetOrAdd(
                authority,
                static key => new HttpClient
                {
                    BaseAddress = new Uri(key)
                });
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
                client.Dispose();
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
