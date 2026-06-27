using Titanic.Db.Enums;
using Titanic.Entity.Attributes;

namespace Titanic.Test.Entity;

/// <summary>
/// Тестовая сущность для сравнительных замеров скорости разных событийных transport-слоёв.
/// </summary>
[Entity("event_layer_speed_entities")]
public sealed class OrmEventLayerSpeedEntity
{
    /// <summary>
    /// Идентификатор записи.
    /// </summary>
    [PrimaryColumn("id", DataValueType.Guid)]
    public int Id { get; set; }

    /// <summary>
    /// Основное отображаемое имя.
    /// </summary>
    [DisplayColumn("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Описание, которое заполняет listener.
    /// </summary>
    [StringColumn("description")]
    public string? Description { get; set; }
}
