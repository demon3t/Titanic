using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Google.Protobuf.WellKnownTypes;
using Titanic.Common.Session;
using Titanic.Entity.Events.Grpc;
using Titanic.Entity.Interfaces;

namespace Titanic.Entity.Events
{
    /// <summary>
    /// Исполнитель удалённых вызовов обработчика событий Entity ORM.
    /// </summary>
    internal static class EntityEventRemoteExecutor
    {
        #region Members

        /// <summary>
        /// Инициализирует новый экземпляр Dispatch.
        /// </summary>
        internal static void Dispatch(global::Titanic.Entity.Orm.Entity entity, BaseEntityManager manager, EntityEventStage stage, IServiceProvider services)
        {
            ArgumentNullException.ThrowIfNull(entity);
            ArgumentNullException.ThrowIfNull(manager);
            ArgumentNullException.ThrowIfNull(services);

            var listener = manager.EventListener
                ?? throw new InvalidOperationException("Event listener location is not configured.");
            var request = CreateRequest(entity, manager, stage);
            var uri = ParseListenerUri(listener);

            EntityEventDispatchResponse response = uri.Scheme.ToLowerInvariant() switch
            {
                "http" or "https" => DispatchHttp(request, ResolveHttpDispatchUri(uri), services),
                "grpc" or "grpcs" => DispatchGrpc(request, NormalizeGrpcUri(uri), services),
                _ => throw new InvalidOperationException(
                    $"Unsupported event listener scheme '{uri.Scheme}'. Use http, https, grpc or grpcs.")
            };

            ApplyResponseValues(entity, response);
        }

        /// <summary>
        /// Инициализирует новый экземпляр DispatchHttp.
        /// </summary>
        private static EntityEventDispatchResponse DispatchHttp(EntityEventDispatchRequest request, Uri dispatchUri, IServiceProvider services)
        {
            var factory = services.GetService(typeof(IEntityEventHttpClientFactory)) as IEntityEventHttpClientFactory
                ?? new DefaultEntityEventHttpClientFactory();
            using var client = factory.CreateClient(dispatchUri);
            using var response = client.PostAsJsonAsync(dispatchUri, request).GetAwaiter().GetResult();

            var body = response.Content.ReadFromJsonAsync<EntityEventDispatchResponse>().GetAwaiter().GetResult();
            body ??= new EntityEventDispatchResponse
            {
                Success = response.IsSuccessStatusCode,
                ErrorMessage = response.ReasonPhrase
            };

            EnsureResponseSuccess(body, response.StatusCode);
            return body;
        }

        /// <summary>
        /// Инициализирует новый экземпляр DispatchGrpc.
        /// </summary>
        private static EntityEventDispatchResponse DispatchGrpc(EntityEventDispatchRequest request, Uri listenerUri, IServiceProvider services)
        {
            var factory = services.GetService(typeof(IEntityEventGrpcClientFactory)) as IEntityEventGrpcClientFactory
                ?? new DefaultEntityEventGrpcClientFactory();
            var client = factory.CreateClient(listenerUri);
            var response = client.Dispatch(ToGrpcRequest(request));
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
        /// Инициализирует новый экземпляр CreateRequest.
        /// </summary>
        private static EntityEventDispatchRequest CreateRequest(global::Titanic.Entity.Orm.Entity entity, BaseEntityManager manager, EntityEventStage stage)
        {
            return new EntityEventDispatchRequest
            {
                ManagerName = manager.Name,
                TableName = entity.TableName,
                Stage = stage.ToString(),
                IsNew = entity.IsNew,
                UserConnection = CloneUserConnection(entity.UserConnection),
                Values = entity.ToDictionary()
            };
        }

        /// <summary>
        /// Инициализирует новый экземпляр CloneUserConnection.
        /// </summary>
        private static UserConnection CloneUserConnection(UserConnection userConnection)
        {
            return new UserConnection
            {
                UserId = userConnection.UserId,
                Culture = new UserCulture
                {
                    Id = userConnection.Culture.Id,
                    Name = userConnection.Culture.Name
                }
            };
        }

        /// <summary>
        /// Инициализирует новый экземпляр ParseListenerUri.
        /// </summary>
        private static Uri ParseListenerUri(string listener)
        {
            if (!Uri.TryCreate(listener, UriKind.Absolute, out var uri))
            {
                throw new InvalidOperationException(
                    $"Event listener '{listener}' is not a valid absolute URI.");
            }

            return uri;
        }

        /// <summary>
        /// Инициализирует новый экземпляр ResolveHttpDispatchUri.
        /// </summary>
        private static Uri ResolveHttpDispatchUri(Uri uri)
        {
            if (!string.IsNullOrWhiteSpace(uri.AbsolutePath) && uri.AbsolutePath != "/")
            {
                return uri;
            }

            return new Uri(uri, EntityEventListenerApiDefaults.HttpDispatchPath);
        }

        /// <summary>
        /// Инициализирует новый экземпляр NormalizeGrpcUri.
        /// </summary>
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
        /// Инициализирует новый экземпляр ToGrpcRequest.
        /// </summary>
        private static EntityEventGrpcRequest ToGrpcRequest(EntityEventDispatchRequest request)
        {
            var result = new EntityEventGrpcRequest
            {
                ManagerName = request.ManagerName,
                TableName = request.TableName,
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

            return result;
        }

        /// <summary>
        /// Инициализирует новый экземпляр ToGrpcValue.
        /// </summary>
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
        /// Инициализирует новый экземпляр FromGrpcValue.
        /// </summary>
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
        /// Инициализирует новый экземпляр TryRestoreNumber.
        /// </summary>
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

        /// <summary>
        /// Инициализирует новый экземпляр ApplyResponseValues.
        /// </summary>
        private static void ApplyResponseValues(global::Titanic.Entity.Orm.Entity entity, EntityEventDispatchResponse response)
        {
            if (!response.Success || response.Values.Count == 0)
            {
                return;
            }

            entity.SetValues(NormalizeValues(response.Values));
        }

        /// <summary>
        /// Инициализирует новый экземпляр NormalizeValues.
        /// </summary>
        private static Dictionary<string, object?> NormalizeValues(IReadOnlyDictionary<string, object?> values)
        {
            return values.ToDictionary(
                x => x.Key,
                x => NormalizeJsonValue(x.Value),
                StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Инициализирует новый экземпляр NormalizeJsonValue.
        /// </summary>
        private static object? NormalizeJsonValue(object? value)
        {
            return value is JsonElement element
                ? NormalizeJsonElement(element)
                : value;
        }

        /// <summary>
        /// Инициализирует новый экземпляр NormalizeJsonElement.
        /// </summary>
        private static object? NormalizeJsonElement(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Null => null,
                JsonValueKind.Undefined => null,
                JsonValueKind.String => element.GetString(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number when element.TryGetInt32(out var intValue) => intValue,
                JsonValueKind.Number when element.TryGetInt64(out var longValue) => longValue,
                JsonValueKind.Number when element.TryGetDecimal(out var decimalValue) => decimalValue,
                JsonValueKind.Number => element.GetDouble(),
                _ => throw new NotSupportedException(
                    $"JSON value kind '{element.ValueKind}' is not supported in entity event response.")
            };
        }

        /// <summary>
        /// Инициализирует новый экземпляр EnsureResponseSuccess.
        /// </summary>
        private static void EnsureResponseSuccess(EntityEventDispatchResponse response, HttpStatusCode statusCode)
        {
            if (response.Success)
            {
                return;
            }

            if (response.Canceled || statusCode == HttpStatusCode.Conflict)
            {
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(response.CancelReason)
                        ? "Entity event pipeline was canceled by external listener."
                        : response.CancelReason);
            }

            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(response.ErrorMessage)
                    ? "External entity event listener failed."
                    : response.ErrorMessage);
        }

        #endregion Members
    }
}
