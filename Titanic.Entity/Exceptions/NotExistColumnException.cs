namespace Titanic.Entity.Exceptions
{
    /// <summary>
    /// Ошибка несуществующей колонки.
    /// </summary>
    public class NotExistColumnException : Exception
    {
        /// <summary>
        /// Шаблон ошибки.
        /// </summary>
        private const string ErrorMessageTemplate = "Column \"{0}\" not found in table \"{1}\".";

        /// <summary>
        /// Конструктор с параметрами.
        /// </summary>
        /// <param name="tableName"> Название таблицы. </param>
        /// <param name="columnName"> Название колонки. </param>
        public NotExistColumnException(string tableName, string columnName)
        : base(string.Format(ErrorMessageTemplate, columnName, tableName))
        {

        }
    }
}
