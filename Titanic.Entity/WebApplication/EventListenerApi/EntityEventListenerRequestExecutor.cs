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

            return ExecuteStages(manager, request, GetRequestStages(request), releaseOnFinalStage: true);
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
                return Success(CreateEntityFromRequest(manager, request));
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
                return Success(CreateEntityFromRequest(manager, request));
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
            return ExecuteStage(manager, request, stage, releaseOnFinalStage: true);
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
            return ExecuteStages(manager, request, [stage], releaseOnFinalStage);
        }

        /// <summary>
        /// Выполняет последовательность стадий событийного pipeline на одном reconstructed Entity.
        /// </summary>
        /// <param name="manager">Менеджер Entity ORM.</param>
        /// <param name="request">Dispatch-запрос события.</param>
        /// <param name="stages">Последовательность стадий для одного transport-вызова.</param>
        /// <param name="releaseOnFinalStage">Удалять listener после финальной стадии legacy dispatch-вызова.</param>
        /// <returns>Результат обработки события.</returns>
        private static EntityEventDispatchResponse ExecuteStages(
            BaseEntityManager manager,
            EntityEventDispatchRequest request,
            IReadOnlyList<EntityEventStage> stages,
            bool releaseOnFinalStage)
        {
            var releaseListeners = false;
            try
            {
                var entity = CreateEntityFromRequest(manager, request);
                var listeners = EntityEventRemoteListenerCache.GetListeners(manager, request);

                foreach (var stage in stages)
                {
                    request.Stage = stage;
                    BaseEntityEventProvider.DispatchListeners(entity, manager, stage, listeners);
                }

                var finalStage = stages[^1];
                releaseListeners = releaseOnFinalStage && BaseEntityEventProvider.IsFinalStage(finalStage);
                return Success(entity);
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
        /// Возвращает список стадий из transport-запроса, сохраняя обратную совместимость со старыми single-stage вызовами.
        /// </summary>
        /// <param name="request">Dispatch-запрос события.</param>
        /// <returns>Последовательность стадий для выполнения.</returns>
        private static IReadOnlyList<EntityEventStage> GetRequestStages(EntityEventDispatchRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            return request.Stages.Count > 0
                ? request.Stages
                : [request.Stage];
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
            return EntityEventGrpcContractMapper.FromGrpcRequest(request);
        }

        /// <summary>
        /// Восстанавливает ORM-сущность из transport-запроса, сохраняя алиасы, пути и display-значения.
        /// </summary>
        /// <param name="manager">Менеджер Entity ORM.</param>
        /// <param name="request">Transport-запрос.</param>
        /// <returns>Восстановленная ORM-сущность.</returns>
        private static global::Titanic.Entity.Orm.Entity CreateEntityFromRequest(
            BaseEntityManager manager,
            EntityEventDispatchRequest request)
        {
            var snapshot = EntityEventSnapshotSerializer.Normalize(request.Entity);
            var entity = manager.Create(
                request.TableName,
                request.UserConnection,
                snapshot?.IsNew ?? request.IsNew);

            if (snapshot != null)
            {
                entity.ApplyTransportSnapshot(snapshot);
                return entity;
            }

            entity.SetOldValues(BaseEntityEventProvider.NormalizeValues(request.OldValues));
            entity.SetValues(BaseEntityEventProvider.NormalizeValues(request.Values));
            return entity;
        }

        /// <summary>
        /// Создаёт успешный transport-ответ.
        /// </summary>
        /// <param name="values">Значения сущности.</param>
        /// <returns>Успешный ответ.</returns>
        private static EntityEventDispatchResponse Success(global::Titanic.Entity.Orm.Entity entity)
        {
            var snapshot = entity.CreateTransportSnapshot();
            return new EntityEventDispatchResponse
            {
                Success = true,
                Entity = snapshot,
                Values = entity.ToDictionary().ToDictionary(
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
