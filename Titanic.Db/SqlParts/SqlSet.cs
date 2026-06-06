namespace Titanic.Db.Abstractions
{
    /// <summary>
    /// SET часть UPDATE запроса.
    /// </summary>
    public sealed record SqlSet(QueryExpression Column, QueryExpression Value);
}
