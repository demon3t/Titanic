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
            return ExecuteForManager(request, manager => Execute(manager, request), cancelInvalidOperation: true);
        }

        /// <summary>
        /// Создаёт экземпляр remote listener-а по имени менеджера из запроса.
        /// </summary>
        /// <param name="request">Dispatch-запрос события.</param>
        /// <returns>Результат создания экземпляра listener-а.</returns>
        internal static EntityEventDispatchResponse Create(EntityEventDispatchRequest request)
        {
            return ExecuteForManager(request, manager => Create(manager, request), cancelInvalidOperation: false);
        }

        /// <summary>
        /// Удаляет экземпляр remote listener-а по имени менеджера из запроса.
        /// </summary>
        /// <param name="request">Dispatch-запрос события.</param>
        /// <returns>Результат удаления экземпляра listener-а.</returns>
        internal static EntityEventDispatchResponse Delete(EntityEventDispatchRequest request)
        {
            return ExecuteForManager(request, manager => Delete(manager, request), cancelInvalidOperation: false);
        }

        /// <summary>
        /// Выполняет конкретную стадию событийного pipeline по имени менеджера из запроса.
        /// </summary>
        /// <param name="request">Dispatch-запрос события.</param>
        /// <param name="stage">Стадия событийного pipeline.</param>
        /// <returns>Результат обработки события.</returns>
        internal static EntityEventDispatchResponse ExecuteStage(EntityEventDispatchRequest request, EntityEventStage stage)
        {
            return ExecuteForManager(
                request,
                manager => ExecuteStage(manager, request, stage),
                cancelInvalidOperation: true);
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

            return ExecuteStage(manager, request, ParseStage(request.Stage), releaseOnFinalStage: true);
        }

        /// <summary>
        /// Создаёт экземпляр remote listener-а для указанного менеджера.
        /// </summary>
        /// <param name="manager">Менеджер Entity ORM.</param>
        /// <param name="request">Dispatch-запрос события.</param>
        /// <returns>Результат создания экземпляра listener-а.</returns>
        internal static EntityEventDispatchResponse Create(BaseEntityManager manager, EntityEventDispatchRequest request)
        {
            ArgumentNullException.ThrowIfNull(manager);
            ArgumentNullException.ThrowIfNull(request);

            try
            {
                EntityEventRemoteListenerCache.Create(manager, request);
                return Success(request.Values);
            }
            catch (Exception ex)
            {
                return Error(ex);
            }
        }

        /// <summary>
        /// Удаляет экземпляр remote listener-а для указанного менеджера.
        /// </summary>
        /// <param name="manager">Менеджер Entity ORM.</param>
        /// <param name="request">Dispatch-запрос события.</param>
        /// <returns>Результат удаления экземпляра listener-а.</returns>
        internal static EntityEventDispatchResponse Delete(BaseEntityManager manager, EntityEventDispatchRequest request)
        {
            ArgumentNullException.ThrowIfNull(manager);
            ArgumentNullException.ThrowIfNull(request);

            try
            {
                EntityEventRemoteListenerCache.Release(manager, request);
                return Success(request.Values);
            }
            catch (Exception ex)
            {
                return Error(ex);
            }
        }

        /// <summary>
        /// Выполняет конкретную стадию событийного pipeline для указанного менеджера.
        /// </summary>
        /// <param name="manager">Менеджер Entity ORM.</param>
        /// <param name="request">Dispatch-запрос события.</param>
        /// <param name="stage">Стадия событийного pipeline.</param>
        /// <returns>Результат обработки события.</returns>
        internal static EntityEventDispatchResponse ExecuteStage(
            BaseEntityManager manager,
            EntityEventDispatchRequest request,
            EntityEventStage stage)
        {
            return ExecuteStage(manager, request, stage, releaseOnFinalStage: false);
        }

        /// <summary>
        /// Выполняет конкретную стадию событийного pipeline.
        /// </summary>
        /// <param name="manager">Менеджер Entity ORM.</param>
        /// <param name="request">Dispatch-запрос события.</param>
        /// <param name="stage">Стадия событийного pipeline.</param>
        /// <param name="releaseOnFinalStage">Удалять listener после финальной стадии legacy dispatch-вызова.</param>
        /// <returns>Результат обработки события.</returns>
        private static EntityEventDispatchResponse ExecuteStage(
            BaseEntityManager manager,
            EntityEventDispatchRequest request,
            EntityEventStage stage,
            bool releaseOnFinalStage)
        {
            var releaseListeners = false;
            try
            {
                request.Stage = stage.ToString();

                var entity = manager.Create(request.TableName, request.UserConnection, request.IsNew);
                entity.SetOldValues(BaseEntityEventProvider.NormalizeValues(request.OldValues));
                entity.SetValues(BaseEntityEventProvider.NormalizeValues(request.Values));

                var listeners = EntityEventRemoteListenerCache.GetListeners(manager, request);
                BaseEntityEventProvider.DispatchListeners(entity, manager, stage, listeners);

                releaseListeners = releaseOnFinalStage && BaseEntityEventProvider.IsFinalStage(stage);
                return Success(entity.ToDictionary());
            }
            catch (InvalidOperationException ex)
            {
                releaseListeners = true;
                return Canceled(ex);
            }
            catch (Exception ex)
            {
                releaseListeners = true;
                return Error(ex);
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
        /// Находит менеджер из запроса и выполняет действие listener API.
        /// </summary>
        /// <param name="request">Dispatch-запрос события.</param>
        /// <param name="execute">Действие, которое нужно выполнить для найденного менеджера.</param>
        /// <param name="cancelInvalidOperation">Возвращать отмену pipeline для ошибок бизнес-валидации.</param>
        /// <returns>Результат действия listener API.</returns>
        private static EntityEventDispatchResponse ExecuteForManager(
            EntityEventDispatchRequest request,
            Func<BaseEntityManager, EntityEventDispatchResponse> execute,
            bool cancelInvalidOperation)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(execute);

            try
            {
                var manager = global::Titanic.Entity.EntityManager.GetManager(request.ManagerName);
                return execute(manager);
            }
            catch (InvalidOperationException ex) when (cancelInvalidOperation)
            {
                return Canceled(ex);
            }
            catch (Exception ex)
            {
                return Error(ex);
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
                    StringComparer.OrdinalIgnoreCase),
                OldValues = request.OldValues.ToDictionary(
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

        /// <summary>
        /// Создаёт успешный transport-ответ.
        /// </summary>
        /// <param name="values">Значения сущности.</param>
        /// <returns>Успешный ответ.</returns>
        private static EntityEventDispatchResponse Success(IReadOnlyDictionary<string, object?> values)
        {
            return new EntityEventDispatchResponse
            {
                Success = true,
                Values = values.ToDictionary(
                    x => x.Key,
                    x => x.Value,
                    StringComparer.OrdinalIgnoreCase)
            };
        }

        /// <summary>
        /// Создаёт transport-ответ отмены pipeline.
        /// </summary>
        /// <param name="ex">Исключение с причиной отмены.</param>
        /// <returns>Ответ отмены pipeline.</returns>
        private static EntityEventDispatchResponse Canceled(Exception ex)
        {
            return new EntityEventDispatchResponse
            {
                Success = false,
                Canceled = true,
                CancelReason = ex.Message
            };
        }

        /// <summary>
        /// Создаёт transport-ответ ошибки.
        /// </summary>
        /// <param name="ex">Исключение, возникшее при обработке.</param>
        /// <returns>Ответ ошибки.</returns>
        private static EntityEventDispatchResponse Error(Exception ex)
        {
            return new EntityEventDispatchResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }

        #endregion Members
    }
}
