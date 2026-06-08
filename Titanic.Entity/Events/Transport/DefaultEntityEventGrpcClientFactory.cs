using Grpc.Net.Client;
using Titanic.Entity.Events.Grpc;

namespace Titanic.Entity.Events
{
    /// <summary>
    /// Стандартная фабрика gRPC-клиентов событийного слоя.
    /// </summary>
    public sealed class DefaultEntityEventGrpcClientFactory : IEntityEventGrpcClientFactory
    {
        #region Members

        /// <inheritdoc />
        public EntityEventListenerGrpc.EntityEventListenerGrpcClient CreateClient(Uri listenerUri)
        {
            ArgumentNullException.ThrowIfNull(listenerUri);

            var channel = GrpcChannel.ForAddress(listenerUri);
            return new EntityEventListenerGrpc.EntityEventListenerGrpcClient(channel);
        }

        #endregion Members
    }
}
