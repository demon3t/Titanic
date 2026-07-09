namespace Titanic.Db.Enums
{
    /// <summary>
    /// Logical operator that joins several query expressions into one SQL condition.
    /// </summary>
    public enum LogicalOperator
    {
        /// <summary>
        /// Requires every child expression to be true.
        /// </summary>
        And = 0,

        /// <summary>
        /// Requires at least one child expression to be true.
        /// </summary>
        Or = 1,
    }
}
