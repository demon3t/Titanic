using Titanic.Db.Enums;

namespace Titanic.Entity.Attributes
{
    /// <summary>
    /// Текстовая колонка Entity ORM.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class StringColumnAttribute : ColumnAttribute
    {
        /// <summary>
        /// Создать описание текстовой колонки.
        /// </summary>
        /// <param name="column">Имя колонки в таблице.</param>
        /// <param name="isLocalized">Признак локализуемой текстовой колонки.</param>
        public StringColumnAttribute(string column, bool isLocalized = false)
            : base(column, DataValueType.String, isLocalized)
        {
        }
    }
}
