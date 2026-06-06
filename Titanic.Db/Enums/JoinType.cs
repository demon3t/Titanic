namespace Titanic.Db.Enums
{
    /// <summary>
    /// Тип SQL JOIN.
    /// </summary>
    public enum JoinType
    {
        /// <summary>
        /// INNER JOIN.
        /// </summary>
        Inner = 0,

        /// <summary>
        /// LEFT JOIN.
        /// </summary>
        Left = 1,

        /// <summary>
        /// RIGHT JOIN.
        /// </summary>
        Right = 2,

        /// <summary>
        /// FULL JOIN.
        /// </summary>
        Full = 3,

        /// <summary>
        /// CROSS JOIN.
        /// </summary>
        Cross = 4
    }
}
