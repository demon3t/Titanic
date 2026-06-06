using Titanic.Common.Session;
using Titanic.Db.Abstractions;
using Titanic.Entity.Strurture;

namespace Titanic.Entity.Orm
{
    /// <summary>
    /// Generic convenience wrapper over EntitySchemaQuery.
    /// </summary>
    public class EntitySchemaQuery<TRootEntity> : EntitySchemaQuery
    {
        public EntitySchemaQuery(BaseDbProvider provider, UserConnection userConnection)
            : base(provider, typeof(TRootEntity), userConnection)
        {
        }

        internal EntitySchemaQuery(
            BaseDbProvider provider,
            EntityStructureScope structureScope,
            UserConnection userConnection)
            : base(provider, structureScope, typeof(TRootEntity), userConnection)
        {
        }
    }
}
