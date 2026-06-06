namespace Titanic.Db.Abstractions
{
    /// <summary>
    /// SET С‡Р°СЃС‚СЊ UPDATE Р·Р°РїСЂРѕСЃР°.
    /// </summary>
    public sealed record SqlSet(QueryExpression Column, QueryExpression Value);
}
