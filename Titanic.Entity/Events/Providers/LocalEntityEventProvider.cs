using Titanic.Entity.Interfaces;

namespace Titanic.Entity.Events
{
    /// <summary>
    /// Provider локального вызова обработчиков событий Entity ORM.
    /// </summary>
    public sealed class LocalEntityEventProvider : BaseEntityEventProvider
    {
        #region Members

        /// <inheritdoc />
        public override bool CanDispatch(BaseEntityManager manager)
        {
            ArgumentNullException.ThrowIfNull(manager);

            return manager.HasLocalEventListener;
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

            EntityEventLocalExecutor.Dispatch(entity, manager, stage);
        }

        #endregion Members
    }
}
