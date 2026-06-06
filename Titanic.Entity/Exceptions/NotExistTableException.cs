namespace Titanic.Entity.Exceptions
{
    /// <summary>
    /// Ошибка несуществующей таблицы.
    /// </summary>
    public class NotExistTableException : Exception
    {
        /// <summary>
        /// Шаблон ошибки.
        /// </summary>
        private const string ErrorMessageTemplate = "Table \"{0}\" not found.";

        /// <summary>
        /// Конструктор с параметрами.
        /// </summary>
        /// <param name="tableName"> Название таблицы. </param>
        public NotExistTableException(string tableName)
        : base(string.Format(ErrorMessageTemplate, tableName))
        {

        }
    }
}
