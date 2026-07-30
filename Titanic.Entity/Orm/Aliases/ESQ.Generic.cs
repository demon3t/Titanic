using Titanic.Common.Session;
using Titanic.Db.Abstractions;

namespace Titanic.Entity.Orm
{
    /// <summary>
    /// Backward-compatible generic short alias for EntitySchemaQuery.
    /// </summary>
    [Obsolete("Deprecated; RemoveIn=1.4.0; Replacement=EntitySchemaQuery<T>")]
    public sealed class ESQ<TRootEntity> : EntitySchemaQuery<TRootEntity>
    {
        public ESQ(BaseDbProvider provider, UserConnection userConnection)
            : base(provider, userConnection)
        {
        }
    }
}
