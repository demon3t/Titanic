using Titanic.Db.Enums;
using Titanic.Entity.Attributes;

namespace Titanic.Test.Entity.Hidden;

[Entity("hidden_scoped_entities")]
public sealed class OrmHiddenScopedEntity
{
    [PrimaryColumn("id", DataValueType.Guid)]
    public int Id { get; set; }

    [DisplayColumn("name")]
    public string Name { get; set; } = string.Empty;
}
