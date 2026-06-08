using Titanic.Entity.Interfaces;

namespace Titanic.Entity.Events
{
    /// <summary>
    /// Локальный исполнитель pipeline событийного слоя.
    /// </summary>
    internal static class EntityEventLocalExecutor
    {
        #region Members

        /// <summary>
        /// Вызывает локальные обработчики для указанной стадии события.
        /// </summary>
        /// <param name="entity">Текущая ORM-сущность.</param>
        /// <param name="manager">Менеджер Entity ORM.</param>
        /// <param name="stage">Стадия событийного pipeline.</param>
        internal static void Dispatch(global::Titanic.Entity.Orm.Entity entity, BaseEntityManager manager, EntityEventStage stage)
        {
            ArgumentNullException.ThrowIfNull(entity);
            ArgumentNullException.ThrowIfNull(manager);

            Dispatch(entity, manager, stage, EntityEventListenerRegistry.GetListeners(entity.TableName));
        }

        /// <summary>
        /// Вызывает переданные экземпляры обработчиков для указанной стадии события.
        /// </summary>
        /// <param name="entity">Текущая ORM-сущность.</param>
        /// <param name="manager">Менеджер Entity ORM.</param>
        /// <param name="stage">Стадия событийного pipeline.</param>
        /// <param name="listeners">Экземпляры обработчиков событий.</param>
        internal static void Dispatch(
            global::Titanic.Entity.Orm.Entity entity,
            BaseEntityManager manager,
            EntityEventStage stage,
            IReadOnlyCollection<BaseEntityEventListener> listeners)
        {
            ArgumentNullException.ThrowIfNull(entity);
            ArgumentNullException.ThrowIfNull(manager);
            ArgumentNullException.ThrowIfNull(listeners);

            if (listeners.Count == 0)
            {
                return;
            }

            var args = new EntityEventArgs(manager, stage);
            foreach (var listener in listeners)
            {
                Invoke(listener, entity, args);

                if (args.IsCanceled)
                {
                    throw new InvalidOperationException(
                        string.IsNullOrWhiteSpace(args.CancelReason)
                            ? $"Entity event pipeline was canceled during '{stage}'."
                            : args.CancelReason);
                }
            }
        }

        /// <summary>
        /// Вызывает метод listener-а, соответствующий стадии события.
        /// </summary>
        /// <param name="listener">Обработчик события.</param>
        /// <param name="entity">Текущая ORM-сущность.</param>
        /// <param name="args">Аргументы события.</param>
        private static void Invoke(BaseEntityEventListener listener, global::Titanic.Entity.Orm.Entity entity, EntityEventArgs args)
        {
            switch (args.Stage)
            {
                case EntityEventStage.Saving:
                    listener.OnSaving(entity, args);
                    break;
                case EntityEventStage.Saved:
                    listener.OnSaved(entity, args);
                    break;
                case EntityEventStage.Inserting:
                    listener.OnInserting(entity, args);
                    break;
                case EntityEventStage.Inserted:
                    listener.OnInserted(entity, args);
                    break;
                case EntityEventStage.Updating:
                    listener.OnUpdating(entity, args);
                    break;
                case EntityEventStage.Updated:
                    listener.OnUpdated(entity, args);
                    break;
                case EntityEventStage.Deleting:
                    listener.OnDeleting(entity, args);
                    break;
                case EntityEventStage.Deleted:
                    listener.OnDeleted(entity, args);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(args), args.Stage, "Unsupported entity event stage.");
            }
        }

        #endregion Members
    }
}
