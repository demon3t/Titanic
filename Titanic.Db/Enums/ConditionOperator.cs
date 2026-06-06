namespace Titanic.Db.Enums
{
    /// <summary>
    /// Оператор условия.
    /// </summary>
    public enum ConditionOperator
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
        /// LIKE.
        /// </summary>
        Like = 8,

        /// <summary>
        /// NOT LIKE.
        /// </summary>
        NotLike = 9,

        /// <summary>
        /// PostgreSQL ILIKE.
        /// </summary>
        ILike = 10,

        /// <summary>
        /// Проверка на NULL.
        /// </summary>
        IsNull = 11,

        /// <summary>
        /// Проверка на NOT NULL.
        /// </summary>
        IsNotNull = 12,
    }
}