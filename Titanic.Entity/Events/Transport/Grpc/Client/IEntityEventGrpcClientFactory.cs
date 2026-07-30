using Titanic.Entity.Events.Grpc;

namespace Titanic.Entity.Events
{
    /// <summary>
    /// Фабрика gRPC-клиентов для внешнего событийного слоя.
    /// </summary>
    public interface IEntityEventGrpcClientFactory
    {
        #region Members

        /// <summary>
        /// Создать gRPC-клиент listener-сервиса.
        /// </summary>
        /// <param name="listenerUri"> URI listener-сервиса. </param>
        /// <returns> gRPC-клиент dispatch-сервиса. </returns>
        EntityEventListenerGrpc.EntityEventListenerGrpcClient CreateClient(Uri listenerUri);

        #endregion Members
    }
}
