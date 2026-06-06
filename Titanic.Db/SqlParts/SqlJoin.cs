using Titanic.Db.Enums;

namespace Titanic.Db.Abstractions
{
    /// <summary>
    /// JOIN часть запроса.
    /// </summary>
    public sealed record SqlJoin(JoinType JoinType, string TableName, string? Alias, QueryExpression? On);
}
