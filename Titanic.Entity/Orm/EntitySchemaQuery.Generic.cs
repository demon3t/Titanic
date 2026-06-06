using Titanic.Common.Session;
using Titanic.Db.Abstractions;

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
    }
}
