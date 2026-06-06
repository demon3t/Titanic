using Titanic.Db.Enums;
using Titanic.Entity.Attributes;

namespace Titanic.Test.Entity;

[Entity("departments")]
[DisableLocalization]
public sealed class OrmLocalizationDisabledDepartmentEntity
{
    [PrimaryColumn("id", DataValueType.Guid)]
    public int Id { get; set; }

    [DisplayColumn("name", isLocalized: true)]
    public string Name { get; set; } = string.Empty;
}
