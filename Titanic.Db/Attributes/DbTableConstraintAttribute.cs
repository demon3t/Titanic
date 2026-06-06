namespace Titanic.Db.Attributes
{
    /// <summary>
    /// Описывает table-level constraint для DDL-создания через <see cref="Table" />.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class DbTableConstraintAttribute : Attribute
    {
        #region Constructors

        /// <summary>
        /// Создать атрибут table-level constraint.
        /// </summary>
        /// <param name="type"> Тип constraint. </param>
        /// <param name="columns"> Колонки constraint. </param>
        public DbTableConstraintAttribute(Table.ConstraintType type, params string[] columns)
            : this(null, type, columns)
        {
        }

        /// <summary>
        /// Создать атрибут именованного table-level constraint.
        /// </summary>
        /// <param name="name"> Имя constraint. </param>
        /// <param name="type"> Тип constraint. </param>
        /// <param name="columns"> Колонки constraint. </param>
        public DbTableConstraintAttribute(string? name, Table.ConstraintType type, params string[] columns)
        {
            Name = name;
            Type = type;
            Columns = columns ?? [];
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Имя constraint.
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// Тип constraint.
        /// </summary>
        public Table.ConstraintType Type { get; }

        /// <summary>
        /// Колонки constraint.
        /// </summary>
        public string[] Columns { get; }

        #endregion Properties
    }
}
