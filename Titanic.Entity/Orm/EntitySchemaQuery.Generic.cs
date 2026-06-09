using Titanic.Common.Session;
using Titanic.Db.Abstractions;
using Titanic.Entity.Interfaces;
using Titanic.Entity.Strurture;

namespace Titanic.Entity.Orm
{
    /// <summary>
    /// Типизированная обёртка над EntitySchemaQuery.
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
            UserConnection userConnection,
            BaseEntityManager? manager = null)
            : base(provider, structureScope, typeof(TRootEntity), userConnection, manager)
        {
        }
    }
}
