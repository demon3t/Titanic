using Titanic.Entity.Interfaces;

namespace Titanic.Entity.Events
{
    /// <summary>
    /// Маршрутизатор событийного слоя Entity ORM.
    /// </summary>
    internal static class EntityEventDispatcher
    {
        #region Members

        /// <summary>
        /// Передаёт событие provider-у, который поддерживает текущую конфигурацию менеджера.
        /// </summary>
        /// <param name="entity">Текущая ORM-сущность.</param>
        /// <param name="manager">Менеджер Entity ORM.</param>
        /// <param name="stage">Стадия событийного pipeline.</param>
        internal static void Dispatch(global::Titanic.Entity.Orm.Entity entity, BaseEntityManager manager, EntityEventStage stage)
        {
            ArgumentNullException.ThrowIfNull(entity);
            ArgumentNullException.ThrowIfNull(manager);

            var services = global::Titanic.Entity.EntityManager.TryGetServiceProvider()
                ?? EntityEventProviderResolver.EmptyServices;
            var provider = EntityEventProviderResolver.Resolve(manager, services);
            provider.Dispatch(entity, manager, stage, services);
        }

        #endregion Members
    }
}
