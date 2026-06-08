using System.Text.Json;
using Google.Protobuf.WellKnownTypes;
using Titanic.Common.Session;
using Titanic.Entity.Interfaces;

namespace Titanic.Entity.Events
{
    /// <summary>
    /// Серверный исполнитель dispatch-запросов обработчика событий.
    /// </summary>
    internal static class EntityEventListenerRequestExecutor
    {
        #region Members

        /// <summary>
        /// Выполняет dispatch-запрос по имени менеджера из запроса.
        /// </summary>
        /// <param name="request">Dispatch-запрос события.</param>
        /// <returns>Результат обработки события.</returns>
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
        /// Выполняет dispatch-запрос для указанного менеджера.
        /// </summary>
        /// <param name="manager">Менеджер Entity ORM.</param>
        /// <param name="request">Dispatch-запрос события.</param>
        /// <returns>Результат обработки события.</returns>
        internal static EntityEventDispatchResponse Execute(BaseEntityManager manager, EntityEventDispatchRequest request)
        {
            ArgumentNullException.ThrowIfNull(manager);
            ArgumentNullException.ThrowIfNull(request);

            var releaseListeners = false;
            try
            {
                var stage = ParseStage(request.Stage);
                releaseListeners = IsFinalStage(stage);

                var entity = manager.Create(request.TableName, request.UserConnection, request.IsNew);
                entity.SetValues(request.Values);

                var listeners = EntityEventRemoteListenerCache.GetListeners(manager, request);
                EntityEventLocalExecutor.Dispatch(entity, manager, stage, listeners);

                return new EntityEventDispatchResponse
                {
                    Success = true,
                    Values = entity.ToDictionary()
                };
            }
            catch (InvalidOperationException ex)
            {
                releaseListeners = true;
                return new EntityEventDispatchResponse
                {
                    Success = false,
                    Canceled = true,
                    CancelReason = ex.Message
                };
            }
            catch (Exception ex)
            {
                releaseListeners = true;
                return new EntityEventDispatchResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
            finally
            {
                if (releaseListeners)
                {
                    EntityEventRemoteListenerCache.Release(manager, request);
                }
            }
        }

        /// <summary>
        /// Преобразует gRPC-запрос во внутренний dispatch-контракт.
        /// </summary>
        /// <param name="request">gRPC-запрос события.</param>
        /// <returns>Dispatch-запрос события.</returns>
        internal static EntityEventDispatchRequest FromGrpc(Grpc.EntityEventGrpcRequest request)
        {
            return new EntityEventDispatchRequest
            {
                ManagerName = request.ManagerName,
                TableName = request.TableName,
                DispatchId = request.DispatchId,
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
        /// Преобразует строковое имя стадии в enum.
        /// </summary>
        /// <param name="stage">Строковое имя стадии.</param>
        /// <returns>Стадия событийного pipeline.</returns>
        private static EntityEventStage ParseStage(string stage)
        {
            if (System.Enum.TryParse<EntityEventStage>(stage, true, out var parsed))
            {
                return parsed;
            }

            throw new InvalidOperationException($"Unknown entity event stage '{stage}'.");
        }

        /// <summary>
        /// Проверяет, завершает ли стадия текущий событийный pipeline.
        /// </summary>
        /// <param name="stage">Стадия событийного pipeline.</param>
        /// <returns><see langword="true" />, если listener-ы нужно удалить сразу.</returns>
        private static bool IsFinalStage(EntityEventStage stage)
        {
            return stage is EntityEventStage.Saved or EntityEventStage.Deleted;
        }

        /// <summary>
        /// Преобразует protobuf-значение в CLR-значение.
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
