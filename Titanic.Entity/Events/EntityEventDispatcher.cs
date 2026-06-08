using Titanic.Entity.Interfaces;
using Titanic.Entity.Orm;

namespace Titanic.Entity.Events
{
    /// <summary>
    /// Маршрутизатор событийного слоя между локальным и внешним исполнителем.
    /// </summary>
    internal static class EntityEventDispatcher
    {
        #region Members

        /// <summary>
        /// Инициализирует новый экземпляр Dispatch.
        /// </summary>
        internal static void Dispatch(global::Titanic.Entity.Orm.Entity entity, BaseEntityManager manager, EntityEventStage stage)
        {
            ArgumentNullException.ThrowIfNull(entity);
            ArgumentNullException.ThrowIfNull(manager);

            if (manager.HasLocalEventListener)
            {
                EntityEventLocalExecutor.Dispatch(entity, manager, stage);
                return;
            }

            var services = global::Titanic.Entity.EntityManager.GetServiceProvider();
            EntityEventRemoteExecutor.Dispatch(entity, manager, stage, services);
        }

        #endregion Members
    }
}
