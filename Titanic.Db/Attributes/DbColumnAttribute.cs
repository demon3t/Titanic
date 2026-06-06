namespace Titanic.Db.Attributes
{
    /// <summary>
    /// Описывает колонку таблицы БД для DDL-создания через <see cref="Table" />.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class DbColumnAttribute : Attribute
    {
        #region Constructors

        /// <summary>
        /// Создать атрибут колонки.
        /// </summary>
        /// <param name="name"> Имя колонки. </param>
        /// <param name="type"> SQL-тип колонки. </param>
        public DbColumnAttribute(string name, Table.ColumnType type)
        {
            Name = string.IsNullOrWhiteSpace(name)
                ? throw new ArgumentException("Column name is empty.", nameof(name))
                : name;
            Type = type;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Имя колонки.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// SQL-тип колонки.
        /// </summary>
        public Table.ColumnType Type { get; }

        /// <summary>
        /// Длина для VARCHAR.
        /// </summary>
        public int Length { get; set; } = 255;

        /// <summary>
        /// Precision для NUMERIC.
        /// </summary>
        public int Precision { get; set; } = 10;

        /// <summary>
        /// Scale для NUMERIC.
        /// </summary>
        public int Scale { get; set; } = 2;

        /// <summary>
        /// Признак NOT NULL.
        /// </summary>
        public bool NotNull { get; set; }

        /// <summary>
        /// Признак PRIMARY KEY.
        /// </summary>
        public bool PrimaryKey { get; set; }

        /// <summary>
        /// Признак UNIQUE.
        /// </summary>
        public bool Unique { get; set; }

        /// <summary>
        /// SQL-выражение значения по умолчанию.
        /// </summary>
        public string? DefaultValue { get; set; }

        /// <summary>
        /// SQL-выражение REFERENCES.
        /// </summary>
        public string? References { get; set; }

        #endregion Properties
    }
}
