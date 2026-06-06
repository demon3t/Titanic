namespace Titanic.Db.Abstractions
{
    /// <summary>
    /// Р”Р°РЅРЅС‹Рµ DELETE Р·Р°РїСЂРѕСЃР° РґР»СЏ SQL-РґРІРёР¶РєР°.
    /// </summary>
    public sealed record SqlDeleteParts(
        string TableName,
        string? Alias,
        IReadOnlyList<SqlTable> Using,
        QueryExpression? Where,
        IReadOnlyList<QueryExpression> Returning);
}
