namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Методы расширения для регистрации сервисов Titanic.Common.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        #region Members

        /// <summary>
        /// Зарегистрировать базовые сервисы Titanic.Common.
        /// </summary>
        /// <param name="services"> Коллекция сервисов. </param>
        /// <returns> Коллекция сервисов для цепочки вызовов. </returns>
        public static IServiceCollection AddTitanicCommon(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);
            return services;
        }

        #endregion Members
    }
}
