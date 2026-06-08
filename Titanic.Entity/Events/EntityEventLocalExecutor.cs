using Titanic.Entity.Interfaces;
using Titanic.Entity.Orm;

namespace Titanic.Entity.Events
{
    /// <summary>
    /// Локальный исполнитель pipeline событийного слоя.
    /// </summary>
    internal static class EntityEventLocalExecutor
    {
        #region Members

        /// <summary>
        /// Инициализирует новый экземпляр Dispatch.
        /// </summary>
        internal static void Dispatch(global::Titanic.Entity.Orm.Entity entity, BaseEntityManager manager, EntityEventStage stage)
        {
            ArgumentNullException.ThrowIfNull(entity);
            ArgumentNullException.ThrowIfNull(manager);

            var listeners = EntityEventListenerRegistry.GetListeners(entity.TableName);
            if (listeners.Count == 0)
            {
                return;
            }

            var args = new EntityEventArgs(manager, stage);
            foreach (var listener in listeners)
            {
                switch (stage)
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
                        throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unsupported entity event stage.");
                }

                if (args.IsCanceled)
                {
                    throw new InvalidOperationException(
                        string.IsNullOrWhiteSpace(args.CancelReason)
                            ? $"Entity event pipeline was canceled during '{stage}'."
                            : args.CancelReason);
                }
            }
        }

        #endregion Members
    }
}
