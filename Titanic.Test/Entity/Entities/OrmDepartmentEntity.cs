using Titanic.Db.Enums;
using Titanic.Entity.Attributes;

namespace Titanic.Test.Entity;

[Entity("departments")]
public sealed class OrmDepartmentEntity
{
    [PrimaryColumn("id", DataValueType.Guid)]
    public int Id { get; set; }

    [DisplayColumn("name")]
    public string Name { get; set; } = string.Empty;

    [StringColumn("description")]
    public string? Description { get; set; }
}
