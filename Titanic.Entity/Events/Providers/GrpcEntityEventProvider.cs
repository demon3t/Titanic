using System.Net;
using Titanic.Entity.Events.Grpc;
using Titanic.Entity.Interfaces;

namespace Titanic.Entity.Events
{
    /// <summary>
    /// Provider gRPC-вызова внешнего обработчика событий Entity ORM.
    /// </summary>
    public sealed class GrpcEntityEventProvider : BaseEntityEventProvider
    {
        #region Members

        /// <inheritdoc />
        public override bool CanDispatch(BaseEntityManager manager)
        {
            ArgumentNullException.ThrowIfNull(manager);

            return TryGetListenerUri(manager, out var uri)
                && uri != null
                && (uri.Scheme.Equals("grpc", StringComparison.OrdinalIgnoreCase)
                    || uri.Scheme.Equals("grpcs", StringComparison.OrdinalIgnoreCase));
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
            var grpcRequest = ToGrpcRequest(request);
            var listenerUri = NormalizeGrpcUri(GetListenerUri(manager));
            var client = CreateClient(listenerUri, services);

            // Серверный cache listener-ов сам лениво создаёт экземпляр по DispatchId
            // и освобождает его на финальной стадии pipeline, поэтому отдельные create/delete
            // transport-вызовы не нужны для штатного remote dispatch.
            var response = SendStageGrpc(client, grpcRequest, stage, stages.Count > 1);
            ApplyResponseValues(entity, response);
            MarkBatchedStages(entity, stages);
        }

        /// <summary>
        /// Создаёт gRPC-клиент для внешнего listener-а.
        /// </summary>
        /// <param name="listenerUri">URI gRPC listener-а.</param>
        /// <param name="services">Провайдер сервисов приложения.</param>
        /// <returns>gRPC-клиент listener-а.</returns>
        private static EntityEventListenerGrpc.EntityEventListenerGrpcClient CreateClient(
            Uri listenerUri,
            IServiceProvider services)
        {
            var factory = services.GetService(typeof(IEntityEventGrpcClientFactory)) as IEntityEventGrpcClientFactory
                ?? new DefaultEntityEventGrpcClientFactory();
            return factory.CreateClient(listenerUri);
        }

        /// <summary>
        /// Отправляет запрос конкретной стадии во внешний gRPC listener.
        /// </summary>
        /// <param name="client">gRPC-клиент listener-а.</param>
        /// <param name="request">gRPC-запрос события.</param>
        /// <param name="stage">Стадия событийного pipeline.</param>
        /// <returns>Ответ событийного listener-а.</returns>
        private static EntityEventDispatchResponse SendStageGrpc(
            EntityEventListenerGrpc.EntityEventListenerGrpcClient client,
            EntityEventGrpcRequest request,
            EntityEventStage stage,
            bool useDispatch)
        {
            var response = useDispatch
                ? client.Dispatch(request)
                : stage switch
                {
                    EntityEventStage.Saving => client.OnSaving(request),
                    EntityEventStage.Saved => client.OnSaved(request),
                    EntityEventStage.Inserting => client.OnInserting(request),
                    EntityEventStage.Inserted => client.OnInserted(request),
                    EntityEventStage.Updating => client.OnUpdating(request),
                    EntityEventStage.Updated => client.OnUpdated(request),
                    EntityEventStage.Deleting => client.OnDeleting(request),
                    EntityEventStage.Deleted => client.OnDeleted(request),
                    _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unsupported entity event stage.")
                };

            return ConvertGrpcResponse(response);
        }

        /// <summary>
        /// Преобразует protobuf-ответ в transport-ответ событийного слоя.
        /// </summary>
        /// <param name="response">gRPC-ответ listener-а.</param>
        /// <returns>Transport-ответ событийного слоя.</returns>
        private static EntityEventDispatchResponse ConvertGrpcResponse(EntityEventGrpcResponse response)
        {
            var result = EntityEventGrpcContractMapper.FromGrpcResponse(response);
            EnsureResponseSuccess(result, HttpStatusCode.OK);
            return result;
        }

        /// <summary>
        /// Нормализует grpc/grpcs URI в URI, который принимает gRPC client.
        /// </summary>
        /// <param name="uri">URI listener-а из конфигурации менеджера.</param>
        /// <returns>HTTP/HTTPS URI для gRPC client-а.</returns>
        private static Uri NormalizeGrpcUri(Uri uri)
        {
            var builder = new UriBuilder(uri)
            {
                Scheme = uri.Scheme.Equals("grpcs", StringComparison.OrdinalIgnoreCase) ? Uri.UriSchemeHttps : Uri.UriSchemeHttp
            };

            if ((builder.Scheme == Uri.UriSchemeHttps && builder.Port == 80)
                || (builder.Scheme == Uri.UriSchemeHttp && builder.Port == 443))
            {
                builder.Port = -1;
            }

            return builder.Uri;
        }

        /// <summary>
        /// Преобразует внутренний transport-запрос в protobuf-модель.
        /// </summary>
        /// <param name="request">Внутренний transport-запрос.</param>
        /// <returns>gRPC transport-запрос.</returns>
        private static EntityEventGrpcRequest ToGrpcRequest(EntityEventDispatchRequest request)
        {
            return EntityEventGrpcContractMapper.ToGrpcRequest(request);
        }

        #endregion Members
    }
}
