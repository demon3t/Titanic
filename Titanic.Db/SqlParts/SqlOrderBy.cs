namespace Titanic.Db.Abstractions
{
    /// <summary>
    /// ORDER BY часть запроса.
    /// </summary>
    public sealed record SqlOrderBy(QueryExpression Expression, bool Desc);
}
