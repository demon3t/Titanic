namespace Titanic.Db.Abstractions
{
    /// <summary>
    /// Р”Р°РЅРЅС‹Рµ UPDATE Р·Р°РїСЂРѕСЃР° РґР»СЏ SQL-РґРІРёР¶РєР°.
    /// </summary>
    public sealed record SqlUpdateParts(
        string TableName,
        string? Alias,
        IReadOnlyList<SqlSet> Set,
        IReadOnlyList<SqlTable> From,
        QueryExpression? Where,
        IReadOnlyList<QueryExpression> Returning);
}
