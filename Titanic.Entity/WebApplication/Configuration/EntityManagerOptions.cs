namespace Titanic.Entity.WebApplication.Configuration
{
    /// <summary>
    /// Runtime-опции Entity ORM менеджера.
    /// </summary>
    public sealed class EntityManagerOptions
    {
        #region Properties

        /// <summary>
        /// Максимальное количество строк, которое разрешено считать одним запросом через EntityManager.
        /// </summary>
        public int? MaxReadRowCount { get; set; }

        /// <summary>
        /// Дополнительные строковые опции менеджера, не связанные с пользовательским контекстом.
        /// </summary>
        public Dictionary<string, string> Values { get; set; } = [];

        #endregion Properties

        #region Public Methods

        /// <summary>
        /// Получить строковую опцию по имени.
        /// </summary>
        /// <param name="key"> Имя опции. </param>
        /// <param name="value"> Значение опции. </param>
        /// <returns> <c>true</c>, если опция найдена. </returns>
        public bool TryGetValue(string key, out string? value)
        {
            value = null;
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            return Values.TryGetValue(key, out value)
                && !string.IsNullOrWhiteSpace(value);
        }

        #endregion Public Methods
    }
}