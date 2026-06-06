namespace Titanic.Db.Abstractions
{
    /// <summary>
    /// Данные SELECT запроса для SQL-движка.
    /// </summary>
    public sealed record SqlSelectParts(
        IReadOnlyList<SqlSelectColumn> Columns,
        string? From,
        string? FromAlias,
        IReadOnlyList<SqlJoin> Joins,
        QueryExpression? Where,
        IReadOnlyList<QueryExpression> GroupBy,
        QueryExpression? Having,
        IReadOnlyList<SqlOrderBy> OrderBy,
        int? Limit,
        int? Offset,
        bool Distinct,
        IReadOnlyList<SqlUnion> Unions);
}
