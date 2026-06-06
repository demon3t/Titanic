using Titanic.Db.Enums;

namespace Titanic.Db.Abstractions
{
    /// <summary>
    /// JOIN С‡Р°СЃС‚СЊ Р·Р°РїСЂРѕСЃР°.
    /// </summary>
    public sealed record SqlJoin(JoinType JoinType, string TableName, string? Alias, QueryExpression? On);
}
