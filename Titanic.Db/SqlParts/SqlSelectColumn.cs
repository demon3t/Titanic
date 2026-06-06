namespace Titanic.Db.Abstractions
{
    /// <summary>
    /// Колонка SELECT.
    /// </summary>
    public sealed record SqlSelectColumn(QueryExpression Expression);
}
