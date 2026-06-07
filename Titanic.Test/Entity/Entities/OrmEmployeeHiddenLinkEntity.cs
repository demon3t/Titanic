using Titanic.Db.Enums;
using Titanic.Entity.Attributes;

namespace Titanic.Test.Entity;

[Entity("employees_hidden_link")]
public sealed class OrmEmployeeHiddenLinkEntity
{
    [PrimaryColumn("id", DataValueType.Guid)]
    public int Id { get; set; }

    [DisplayColumn("name")]
    public string Name { get; set; } = string.Empty;

    [ReferenceColumn("hidden_id", "hidden_scoped_entities", DataValueType.Guid)]
    public int? HiddenId { get; set; }
}
