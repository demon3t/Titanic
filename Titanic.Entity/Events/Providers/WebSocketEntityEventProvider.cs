using System.Net;
using Titanic.Entity.Interfaces;

namespace Titanic.Entity.Events
{
    /// <summary>
    /// Provider WebSocket-вызова внешнего обработчика событий Entity ORM.
    /// </summary>
    public sealed class WebSocketEntityEventProvider : BaseEntityEventProvider
    {
        #region Members

        /// <inheritdoc />
        public override bool CanDispatch(BaseEntityManager manager)
        {
            ArgumentNullException.ThrowIfNull(manager);

            return TryGetListenerUri(manager, out var uri)
                && uri != null
                && (uri.Scheme.Equals("ws", StringComparison.OrdinalIgnoreCase)
                    || uri.Scheme.Equals("wss", StringComparison.OrdinalIgnoreCase));
        }

        /// <inheritdoc />
        public override void Dispatch(
            global::Titanic.Entity.Orm.Entity entity,
            BaseEntityManager manager,
            EntityEventStage stage,
            IServiceProvider services)
        {
            ArgumentNullException.ThrowIfNull(entity);
            ArgumentNullException.ThrowIfNull(manager);
            ArgumentNullException.ThrowIfNull(services);

            if (ShouldSkipTransportStage(entity, stage))
            {
                return;
            }

            var request = CreateRequest(entity, manager, stage);
            var stages = GetDispatchStages(entity, stage);
            request.Stage = stages[0];
            request.Stages = [.. stages];
            var client = CreateClient(GetListenerUri(manager), services);
            var response = client.Send(new EntityEventWebSocketRequest
            {
                Action = EntityEventWebSocketAction.Dispatch,
                Request = request
            });

            EnsureResponseSuccess(response.Response, HttpStatusCode.OK);
            ApplyResponseValues(entity, response.Response);
            MarkBatchedStages(entity, stages);
        }

        /// <summary>
        /// Возвращает persistent WebSocket-клиент для внешнего listener API.
        /// </summary>
        /// <param name="listenerUri">URI listener API.</param>
        /// <param name="services">Провайдер сервисов приложения.</param>
        /// <returns>Persistent WebSocket-клиент listener API.</returns>
        private static EntityEventWebSocketClient CreateClient(Uri listenerUri, IServiceProvider services)
        {
            var factory = services.GetService(typeof(IEntityEventWebSocketClientFactory)) as IEntityEventWebSocketClientFactory
                ?? new DefaultEntityEventWebSocketClientFactory();
            return factory.CreateClient(listenerUri);
        }

        #endregion Members
    }
}
