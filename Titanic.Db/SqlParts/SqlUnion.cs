namespace Titanic.Db.Abstractions
{
    /// <summary>
    /// UNION часть запроса.
    /// </summary>
    public sealed record SqlUnion(BaseQuery Query, bool All);
}
