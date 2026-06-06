namespace Titanic.Db.Abstractions
{
    /// <summary>
    /// Р”Р°РЅРЅС‹Рµ SELECT Р·Р°РїСЂРѕСЃР° РґР»СЏ SQL-РґРІРёР¶РєР°.
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
