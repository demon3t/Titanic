using System.Net;
using System.Text.Json;
using Google.Protobuf.WellKnownTypes;
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

            var request = CreateRequest(entity, manager, stage);
            var listenerUri = NormalizeGrpcUri(GetListenerUri(manager));
            var client = CreateClient(listenerUri, services);

            if (IsInitialStage(stage))
            {
                SendLifecycleGrpc(client.Create(ToGrpcRequest(request)));
            }

            try
            {
                var response = SendStageGrpc(client, ToGrpcRequest(request), stage);
                ApplyResponseValues(entity, response);
            }
            catch
            {
                TryDeleteRemoteListener(client, request);
                throw;
            }

            if (IsFinalStage(stage))
            {
                TryDeleteRemoteListener(client, request);
            }
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
            EntityEventStage stage)
        {
            var response = stage switch
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
        /// Проверяет ответ lifecycle-вызова gRPC listener-а.
        /// </summary>
        /// <param name="response">gRPC-ответ listener-а.</param>
        private static void SendLifecycleGrpc(EntityEventGrpcResponse response)
        {
            ConvertGrpcResponse(response);
        }

        /// <summary>
        /// Пытается удалить remote listener, не перекрывая исходный результат обработки события.
        /// </summary>
        /// <param name="client">gRPC-клиент listener-а.</param>
        /// <param name="request">Transport-запрос события.</param>
        private static void TryDeleteRemoteListener(
            EntityEventListenerGrpc.EntityEventListenerGrpcClient client,
            EntityEventDispatchRequest request)
        {
            try
            {
                SendLifecycleGrpc(client.Delete(ToGrpcRequest(request)));
            }
            catch
            {
                // TTL на стороне listener API удалит экземпляр, если явная очистка не дошла.
            }
        }

        /// <summary>
        /// Преобразует protobuf-ответ в transport-ответ событийного слоя.
        /// </summary>
        /// <param name="response">gRPC-ответ listener-а.</param>
        /// <returns>Transport-ответ событийного слоя.</returns>
        private static EntityEventDispatchResponse ConvertGrpcResponse(EntityEventGrpcResponse response)
        {
            var result = new EntityEventDispatchResponse
            {
                Success = response.Success,
                Canceled = response.Canceled,
                CancelReason = string.IsNullOrWhiteSpace(response.CancelReason) ? null : response.CancelReason,
                ErrorMessage = string.IsNullOrWhiteSpace(response.ErrorMessage) ? null : response.ErrorMessage,
                Values = response.Values.ToDictionary(
                    x => x.Key,
                    x => FromGrpcValue(x.Value),
                    StringComparer.OrdinalIgnoreCase)
            };

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
            var result = new EntityEventGrpcRequest
            {
                ManagerName = request.ManagerName,
                TableName = request.TableName,
                DispatchId = request.DispatchId,
                Stage = request.Stage,
                IsNew = request.IsNew,
                UserConnection = new EntityEventGrpcUserConnection
                {
                    UserId = request.UserConnection.UserId.ToString(),
                    Culture = new EntityEventGrpcUserCulture
                    {
                        Id = request.UserConnection.Culture.Id.ToString(),
                        Name = request.UserConnection.Culture.Name
                    }
                }
            };

            foreach (var value in request.Values)
            {
                result.Values.Add(value.Key, ToGrpcValue(value.Value));
            }

            foreach (var value in request.OldValues)
            {
                result.OldValues.Add(value.Key, ToGrpcValue(value.Value));
            }

            return result;
        }

        /// <summary>
        /// Преобразует CLR-значение в protobuf Value.
        /// </summary>
        /// <param name="value">CLR-значение.</param>
        /// <returns>Protobuf-значение.</returns>
        private static Value ToGrpcValue(object? value)
        {
            return value switch
            {
                null => Value.ForNull(),
                bool boolValue => Value.ForBool(boolValue),
                string stringValue => Value.ForString(stringValue),
                Guid guidValue => Value.ForString(guidValue.ToString()),
                DateTime dateTimeValue => Value.ForString(dateTimeValue.ToString("O")),
                DateTimeOffset dateTimeOffsetValue => Value.ForString(dateTimeOffsetValue.ToString("O")),
                byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal
                    => Value.ForNumber(Convert.ToDouble(value)),
                _ => Value.ForString(value.ToString() ?? string.Empty)
            };
        }

        /// <summary>
        /// Преобразует protobuf Value в CLR-значение.
        /// </summary>
        /// <param name="value">Protobuf-значение.</param>
        /// <returns>CLR-значение.</returns>
        private static object? FromGrpcValue(Value value)
        {
            return value.KindCase switch
            {
                Value.KindOneofCase.NullValue => null,
                Value.KindOneofCase.BoolValue => value.BoolValue,
                Value.KindOneofCase.StringValue => value.StringValue,
                Value.KindOneofCase.NumberValue => TryRestoreNumber(value.NumberValue),
                Value.KindOneofCase.StructValue => JsonSerializer.Deserialize<object>(value.StructValue.ToString()),
                Value.KindOneofCase.ListValue => JsonSerializer.Deserialize<object>(value.ListValue.ToString()),
                _ => null
            };
        }

        /// <summary>
        /// Восстанавливает целочисленное значение, если protobuf передал число без дробной части.
        /// </summary>
        /// <param name="value">Числовое значение protobuf.</param>
        /// <returns>CLR-число.</returns>
        private static object TryRestoreNumber(double value)
        {
            if (Math.Abs(value % 1) < double.Epsilon)
            {
                if (value is >= int.MinValue and <= int.MaxValue)
                {
                    return Convert.ToInt32(value);
                }

                if (value is >= long.MinValue and <= long.MaxValue)
                {
                    return Convert.ToInt64(value);
                }
            }

            return value;
        }

        #endregion Members
    }
}
