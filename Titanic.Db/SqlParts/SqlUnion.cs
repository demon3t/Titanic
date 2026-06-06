namespace Titanic.Db.Abstractions
{
    /// <summary>
    /// UNION С‡Р°СЃС‚СЊ Р·Р°РїСЂРѕСЃР°.
    /// </summary>
    public sealed record SqlUnion(BaseQuery Query, bool All);
}
