namespace Titanic.Db.Attributes
{
    /// <summary>
    /// Описывает индекс таблицы БД для создания через <see cref="Table" />.
    /// Можно применять к классу для составных индексов и к свойству для индекса одной колонки.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    public sealed class DbIndexAttribute : Attribute
    {
        #region Constructors

        /// <summary>
        /// Создать атрибут индекса.
        /// </summary>
        /// <param name="name"> Имя индекса. </param>
        /// <param name="columns"> Колонки индекса. Для property-level атрибута можно не задавать. </param>
        public DbIndexAttribute(string name, params string[] columns)
        {
            Name = string.IsNullOrWhiteSpace(name)
                ? throw new ArgumentException("Index name is empty.", nameof(name))
                : name;
            Columns = columns ?? [];
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Имя индекса.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Колонки индекса.
        /// </summary>
        public string[] Columns { get; }

        /// <summary>
        /// Признак уникального индекса.
        /// </summary>
        public bool Unique { get; set; }

        #endregion Properties
    }
}
