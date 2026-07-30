namespace Titanic.Entity.Events
{
    /// <summary>
    /// Фабрика WebSocket-клиентов для внешнего событийного слоя.
    /// </summary>
    public interface IEntityEventWebSocketClientFactory
    {
        #region Members

        /// <summary>
        /// Создаёт или возвращает существующий WebSocket-клиент listener API.
        /// </summary>
        /// <param name="listenerUri">URI listener API.</param>
        /// <returns>Клиент для обмена transport-сообщениями по WebSocket.</returns>
        EntityEventWebSocketClient CreateClient(Uri listenerUri);

        #endregion Members
    }
}
