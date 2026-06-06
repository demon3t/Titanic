namespace Titanic.Db.Abstractions
{
    /// <summary>
    /// Данные UPDATE запроса для SQL-движка.
    /// </summary>
    public sealed record SqlUpdateParts(
        string TableName,
        string? Alias,
        IReadOnlyList<SqlSet> Set,
        IReadOnlyList<SqlTable> From,
        QueryExpression? Where,
        IReadOnlyList<QueryExpression> Returning);
}
