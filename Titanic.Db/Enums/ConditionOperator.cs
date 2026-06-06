namespace Titanic.Db.Enums
{
    /// <summary>
    /// Query comparison operator.
    /// </summary>
    public enum ConditionOperator
    {
        /// <summary>
        /// Equal.
        /// </summary>
        Equal = 0,

        /// <summary>
        /// Not equal.
        /// </summary>
        NotEqual = 1,

        /// <summary>
        /// Greater than.
        /// </summary>
        GreaterThan = 2,

        /// <summary>
        /// Greater than or equal.
        /// </summary>
        GreaterThanOrEqual = 3,

        /// <summary>
        /// Less than.
        /// </summary>
        LessThan = 4,

        /// <summary>
        /// Less than or equal.
        /// </summary>
        LessThanOrEqual = 5,

        /// <summary>
        /// IN.
        /// </summary>
        In = 6,

        /// <summary>
        /// NOT IN.
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

    }
}
