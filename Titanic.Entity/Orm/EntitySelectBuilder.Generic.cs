using Titanic.Common.Session;
using Titanic.Db.Abstractions;
using Titanic.Entity.Interfaces;
using Titanic.Entity.Strurture;

namespace Titanic.Entity.Orm
{
    /// <summary>
    /// Типизированная обёртка над нетипизированным SELECT builder-ом.
    /// </summary>
    public sealed class EntitySelectBuilder<TEntity> : EntitySelectBuilder
    {
        public EntitySelectBuilder(BaseDbProvider provider, UserConnection userConnection)
            : base(provider, typeof(TEntity), userConnection)
        {
        }

        internal EntitySelectBuilder(
            BaseDbProvider provider,
            EntityStructureScope structureScope,
            UserConnection userConnection,
            BaseEntityManager? manager = null)
            : base(provider, structureScope, typeof(TEntity), userConnection, manager)
        {
        }
    }
}
