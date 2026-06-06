namespace Titanic.Entity.Orm
{
    /// <summary>
    /// Тип сравнения фильтра Entity ORM.
    /// </summary>
    public enum EntityComparisonType
    {
        /// <summary>
        /// Равно.
        /// </summary>
        Equal = 0,

        /// <summary>
        /// Не равно.
        /// </summary>
        NotEqual = 1,

        /// <summary>
        /// Больше.
        /// </summary>
        GreaterThan = 2,

        /// <summary>
        /// Больше или равно.
        /// </summary>
        GreaterThanOrEqual = 3,

        /// <summary>
        /// Меньше.
        /// </summary>
        LessThan = 4,

        /// <summary>
        /// Меньше или равно.
        /// </summary>
        LessThanOrEqual = 5,

        /// <summary>
        /// Входит в набор.
        /// </summary>
        In = 6,

        /// <summary>
        /// Не входит в набор.
        /// </summary>
        NotIn = 7,

        /// <summary>
        /// SQL LIKE.
        /// </summary>
        Like = 8,

        /// <summary>
        /// SQL NOT LIKE.
        /// </summary>
        NotLike = 9,

        /// <summary>
        /// Case-insensitive LIKE.
        /// </summary>
        ILike = 10,

        /// <summary>
        /// IS NULL.
        /// </summary>
        IsNull = 11,

        /// <summary>
        /// IS NOT NULL.
        /// </summary>
        IsNotNull = 12,

        /// <summary>
        /// Поиск по вхождению без ручного указания %...%.
        /// </summary>
        Contains = 13,

        /// <summary>
        /// Поиск по началу строки без ручного указания %.
        /// </summary>
        StartsWith = 14,

        /// <summary>
        /// Поиск по концу строки без ручного указания %.
        /// </summary>
        EndsWith = 15,
    }
}
