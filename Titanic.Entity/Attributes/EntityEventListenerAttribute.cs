namespace Titanic.Entity.Attributes
{
    /// <summary>
    /// Атрибут обработчика событий Entity ORM для конкретной сущности.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class EntityEventListenerAttribute : Attribute
    {
        #region Constructors

        /// <summary>
        /// Создать атрибут обработчика событий Entity ORM.
        /// </summary>
        /// <param name="entityName"> Имя сущности, к которой относится обработчик. </param>
        public EntityEventListenerAttribute(string entityName)
        {
            if (string.IsNullOrWhiteSpace(entityName))
            {
                throw new ArgumentException("Entity event listener entity name is empty", nameof(entityName));
            }

            EntityName = entityName;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Имя сущности, к которой относится обработчик.
        /// </summary>
        public string EntityName { get; }

        #endregion Properties
    }
}
