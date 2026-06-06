namespace Titanic.Db.Abstractions
{
    /// <summary>
    /// Р”Р°РЅРЅС‹Рµ INSERT Р·Р°РїСЂРѕСЃР° РґР»СЏ SQL-РґРІРёР¶РєР°.
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
