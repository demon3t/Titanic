namespace Titanic.Entity.Attributes
{
    /// <summary>
    /// Атрибут имени Entity ORM менеджера для классов-оберток <see cref="Interfaces.BaseEntityManager" />.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class EntityManagerConnectionAttribute : Attribute
    {
        #region Constructors

        /// <summary>
        /// Создать атрибут имени Entity ORM менеджера.
        /// </summary>
        /// <param name="name"> Имя Entity ORM менеджера. </param>
        public EntityManagerConnectionAttribute(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Entity manager name is empty", nameof(name));
            }

            Name = name;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Имя Entity ORM менеджера.
        /// </summary>
        public string Name { get; }

        #endregion Properties
    }
}
