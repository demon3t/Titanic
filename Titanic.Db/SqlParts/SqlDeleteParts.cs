namespace Titanic.Db.Abstractions
{
    /// <summary>
    /// Данные DELETE запроса для SQL-движка.
    /// </summary>
    public sealed record SqlDeleteParts(
        string TableName,
        string? Alias,
        IReadOnlyList<SqlTable> Using,
        QueryExpression? Where,
        IReadOnlyList<QueryExpression> Returning);
}
