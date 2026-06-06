namespace Titanic.Db.Abstractions
{
    /// <summary>
    /// ORDER BY С‡Р°СЃС‚СЊ Р·Р°РїСЂРѕСЃР°.
    /// </summary>
    public sealed record SqlOrderBy(QueryExpression Expression, bool Desc);
}
