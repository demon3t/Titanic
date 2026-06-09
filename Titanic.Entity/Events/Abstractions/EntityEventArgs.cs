using Titanic.Entity.Interfaces;

namespace Titanic.Entity.Events
{
    /// <summary>
    /// Аргументы события Entity ORM.
    /// </summary>
    public class EntityEventArgs
    {
        #region Constructors

        /// <summary>
        /// Создать аргументы события Entity ORM.
        /// </summary>
        /// <param name="manager"> Entity ORM менеджер текущей операции. </param>
        /// <param name="stage"> Стадия жизненного цикла. </param>
        /// <param name="cancellationToken"> Токен отмены внешней операции. </param>
        public EntityEventArgs(
            BaseEntityManager manager,
            EntityEventStage stage,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(manager);

            Manager = manager;
            Stage = stage;
            CancellationToken = cancellationToken;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Entity ORM менеджер, внутри которого выполняется операция.
        /// </summary>
        public BaseEntityManager Manager { get; }

        /// <summary>
        /// Стадия жизненного цикла текущего события.
        /// </summary>
        public EntityEventStage Stage { get; }

        /// <summary>
        /// Внешний токен отмены текущей операции.
        /// </summary>
        public CancellationToken CancellationToken { get; }

        /// <summary>
        /// Признак отмены дальнейшей обработки событийного pipeline.
        /// </summary>
        public bool IsCanceled { get; private set; }

        /// <summary>
        /// Причина отмены дальнейшей обработки, если она была указана.
        /// </summary>
        public string? CancelReason { get; private set; }

        #endregion Properties

        #region Public Methods

        /// <summary>
        /// Отменить дальнейшую обработку событий текущей операции.
        /// </summary>
        /// <param name="reason"> Причина отмены. </param>
        public void Cancel(string? reason = null)
        {
            IsCanceled = true;
            CancelReason = reason;
        }

        #endregion Public Methods
    }
}
