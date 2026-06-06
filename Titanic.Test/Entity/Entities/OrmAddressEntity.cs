using Titanic.Db.Enums;
using Titanic.Entity.Attributes;

namespace Titanic.Test.Entity;

[Entity("addresses")]
public sealed class OrmAddressEntity
{
    [PrimaryColumn("id", DataValueType.Guid)]
    public int Id { get; set; }

    [ReferenceColumn("employee_id", "employees", DataValueType.Guid)]
    public int EmployeeId { get; set; }

    [DisplayColumn("city")]
    public string City { get; set; } = string.Empty;

    [StringColumn("street")]
    public string Street { get; set; } = string.Empty;

    [Column("is_primary", DataValueType.String)]
    public bool IsPrimary { get; set; }
}
