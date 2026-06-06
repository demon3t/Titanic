namespace Titanic.Entity.Attributes
{
    /// <summary>
    /// Отключает применение таблицы локализации для сущности или конкретной колонки.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    public sealed class DisableLocalizationAttribute : Attribute
    {
    }
}
