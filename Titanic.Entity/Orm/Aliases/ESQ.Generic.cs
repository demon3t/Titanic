using Titanic.Common.Session;
using Titanic.Db.Abstractions;

namespace Titanic.Entity.Orm
{
    /// <summary>
    /// Backward-compatible generic short alias for EntitySchemaQuery.
    /// </summary>
    [Obsolete("Will be removed in 1.4.0. Use EntitySchemaQuery<T> instead.")]
    public sealed class ESQ<TRootEntity> : EntitySchemaQuery<TRootEntity>
    {
        public ESQ(BaseDbProvider provider, UserConnection userConnection)
            : base(provider, userConnection)
        {
        }
    }
}
