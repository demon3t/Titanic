using Titanic.Db.Enums;
using Titanic.Entity.Attributes;

namespace Titanic.Test.Entity;

[Entity("employees")]
public sealed class OrmEmployeeEntity
{
    [PrimaryColumn("id", DataValueType.Guid)]
    public int Id { get; set; }

    [DisplayColumn("name")]
    public string Name { get; set; } = string.Empty;

    [StringColumn("email")]
    public string Email { get; set; } = string.Empty;

    [ReferenceColumn("department_id", "departments", DataValueType.Guid)]
    public int? DepartmentId { get; set; }

    [Column("salary", DataValueType.String)]
    public decimal Salary { get; set; }

    [Column("is_active", DataValueType.String)]
    public bool IsActive { get; set; } = true;
}
