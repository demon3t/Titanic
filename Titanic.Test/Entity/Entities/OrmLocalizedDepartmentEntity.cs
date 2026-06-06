using Titanic.Db.Enums;
using Titanic.Entity.Attributes;

namespace Titanic.Test.Entity;

[Entity("departments")]
public sealed class OrmLocalizedDepartmentEntity
{
    [PrimaryColumn("id", DataValueType.Guid)]
    public int Id { get; set; }

    [DisplayColumn("name", isLocalized: true)]
    public string Name { get; set; } = string.Empty;

    [StringColumn("description", isLocalized: true)]
    public string? Description { get; set; }
}
