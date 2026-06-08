using System.Text.Json;
using Google.Protobuf.WellKnownTypes;
using Titanic.Common.Session;

namespace Titanic.Entity.Events
{
    /// <summary>
    /// Серверный исполнитель dispatch-запросов обработчика событий.
    /// </summary>
    internal static class EntityEventListenerRequestExecutor
    {
        #region Members

        /// <summary>
        /// Инициализирует новый экземпляр Execute.
        /// </summary>
        internal static EntityEventDispatchResponse Execute(EntityEventDispatchRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            try
            {
                var manager = global::Titanic.Entity.EntityManager.GetManager(request.ManagerName);
                return Execute(manager, request);
            }
            catch (InvalidOperationException ex)
            {
                return new EntityEventDispatchResponse
                {
                    Success = false,
                    Canceled = true,
                    CancelReason = ex.Message
                };
            }
            catch (Exception ex)
            {
                return new EntityEventDispatchResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Инициализирует новый экземпляр Execute.
        /// </summary>
        internal static EntityEventDispatchResponse Execute(Interfaces.BaseEntityManager manager, EntityEventDispatchRequest request)
        {
            ArgumentNullException.ThrowIfNull(manager);
            ArgumentNullException.ThrowIfNull(request);

            try
            {
                var stage = ParseStage(request.Stage);
                var entity = manager.Create(request.TableName, request.UserConnection, request.IsNew);
                entity.SetValues(request.Values);

                EntityEventLocalExecutor.Dispatch(entity, manager, stage);

                return new EntityEventDispatchResponse
                {
                    Success = true,
                    Values = entity.ToDictionary()
                };
            }
            catch (InvalidOperationException ex)
            {
                return new EntityEventDispatchResponse
                {
                    Success = false,
                    Canceled = true,
                    CancelReason = ex.Message
                };
            }
            catch (Exception ex)
            {
                return new EntityEventDispatchResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Инициализирует новый экземпляр FromGrpc.
        /// </summary>
        internal static EntityEventDispatchRequest FromGrpc(Grpc.EntityEventGrpcRequest request)
        {
            return new EntityEventDispatchRequest
            {
                ManagerName = request.ManagerName,
                TableName = request.TableName,
                Stage = request.Stage,
                IsNew = request.IsNew,
                UserConnection = new UserConnection
                {
                    UserId = Guid.Parse(request.UserConnection.UserId),
                    Culture = new UserCulture
                    {
                        Id = Guid.Parse(request.UserConnection.Culture.Id),
                        Name = request.UserConnection.Culture.Name
                    }
                },
                Values = request.Values.ToDictionary(
                    x => x.Key,
                    x => FromGrpcValue(x.Value),
                    StringComparer.OrdinalIgnoreCase)
            };
        }

        /// <summary>
        /// Инициализирует новый экземпляр ParseStage.
        /// </summary>
        private static EntityEventStage ParseStage(string stage)
        {
            if (System.Enum.TryParse<EntityEventStage>(stage, true, out var parsed))
            {
                return parsed;
            }

            throw new InvalidOperationException($"Unknown entity event stage '{stage}'.");
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

        #endregion Members
    }
}
