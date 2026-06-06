namespace Titanic.Entity.Attributes
{
    /// <summary>
    /// Сущности.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class EntityAttribute : Attribute
    {
        /// <summary>
        /// Таблица
        /// </summary>
        public string Table { get; }

        /// <summary>
        /// Является представлением.
        /// </summary>
        public bool IsView { get; }

        /// <summary>
        /// Конструктор с параметрами.
        /// </summary>
        /// <param name="table"> Навзание таблицы. </param>
        /// <param name="isView"> Является представлением. </param>
        public EntityAttribute(string table, bool isView = false)
        {
            Table = table;
            IsView = isView;
        }
    }
}
