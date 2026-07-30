using Titanic.Common.Session;
using Titanic.Db.Abstractions;
using Titanic.Entity.Strurture;

namespace Titanic.Entity.Orm
{
    /// <summary>
    /// Backward-compatible short alias for EntitySchemaQuery.
    /// </summary>
    [Obsolete("Deprecated; RemoveIn=1.4.0; Replacement=EntitySchemaQuery")]
    public class ESQ : EntitySchemaQuery
    {
        public ESQ(BaseDbProvider provider, Type entityType, UserConnection userConnection)
            : base(provider, entityType, userConnection)
        {
        }

        public ESQ(BaseDbProvider provider, string tableName, UserConnection userConnection)
            : base(provider, tableName, userConnection)
        {
        }

        internal ESQ(BaseDbProvider provider, EntityStructure structure, UserConnection userConnection)
            : base(provider, structure, userConnection)
        {
        }
    }
}
