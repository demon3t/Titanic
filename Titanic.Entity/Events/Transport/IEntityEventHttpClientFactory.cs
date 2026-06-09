namespace Titanic.Entity.Events
{
    /// <summary>
    /// Фабрика HTTP-клиентов для внешнего событийного слоя.
    /// </summary>
    public interface IEntityEventHttpClientFactory
    {
        #region Members

        /// <summary>
        /// Создать HTTP-клиент для вызова listener endpoint-а.
        /// </summary>
        /// <param name="listenerUri"> URI listener endpoint-а. </param>
        /// <returns> Настроенный HTTP-клиент. </returns>
        HttpClient CreateClient(Uri listenerUri);

        #endregion Members
    }
}
