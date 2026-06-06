namespace Titanic.Db.Attributes
{
    /// <summary>
    /// Описывает таблицу БД, которую можно создать через <see cref="Table" /> по CLR-модели.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class DbTableAttribute : Attribute
    {
        #region Constructors

        /// <summary>
        /// Создать атрибут таблицы.
        /// </summary>
        /// <param name="name"> Имя таблицы с опциональной схемой. </param>
        public DbTableAttribute(string name)
        {
            Name = string.IsNullOrWhiteSpace(name)
                ? throw new ArgumentException("Table name is empty.", nameof(name))
                : name;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Имя таблицы с опциональной схемой.
        /// </summary>
        public string Name { get; }

        #endregion Properties
    }
}
