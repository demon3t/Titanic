namespace Titanic.Db.Abstractions
{
    /// <summary>
    /// Данные INSERT запроса для SQL-движка.
    /// </summary>
    public sealed record SqlInsertParts(
        string TableName,
        IReadOnlyList<string> Columns,
        IReadOnlyList<IReadOnlyList<QueryExpression>> Rows,
        BaseQuery? Select,
        IReadOnlyList<QueryExpression> Returning,
        bool OnConflictDoNothing,
        IReadOnlyList<string> ConflictColumns,
        IReadOnlyList<SqlSet> ConflictUpdateSet,
        string? ConflictSql);
}
