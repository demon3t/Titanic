using System.Collections.Concurrent;
using Grpc.Net.Client;
using Titanic.Entity.Events.Grpc;

namespace Titanic.Entity.Events
{
    /// <summary>
    /// Стандартная фабрика gRPC-клиентов событийного слоя.
    /// </summary>
    public sealed class DefaultEntityEventGrpcClientFactory : IEntityEventGrpcClientFactory, IDisposable
    {
        #region Fields

        /// <summary>
        /// Кэш gRPC-каналов по адресу listener-а, чтобы переиспользовать уже поднятый HTTP/2 transport.
        /// </summary>
        private readonly ConcurrentDictionary<string, GrpcChannel> _channels = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Признак того, что фабрика уже освобождена и не должна создавать новые каналы.
        /// </summary>
        private bool _disposed;

        #endregion Fields

        #region Members

        /// <inheritdoc />
        public EntityEventListenerGrpc.EntityEventListenerGrpcClient CreateClient(Uri listenerUri)
        {
            ArgumentNullException.ThrowIfNull(listenerUri);

            ThrowIfDisposed();

            // Повторно используем канал для одного и того же listener-а, чтобы не платить
            // за повторную инициализацию HTTP/2 транспорта на каждом событии.
            var channel = _channels.GetOrAdd(
                listenerUri.AbsoluteUri,
                _ => GrpcChannel.ForAddress(listenerUri));
            return new EntityEventListenerGrpc.EntityEventListenerGrpcClient(channel);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            foreach (var channel in _channels.Values)
            {
                channel.Dispose();
            }

            _channels.Clear();
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
