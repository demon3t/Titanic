namespace Titanic.Entity.Events
{
    /// <summary>
    /// Стандартная фабрика HTTP-клиентов событийного слоя.
    /// </summary>
    public sealed class DefaultEntityEventHttpClientFactory : IEntityEventHttpClientFactory
    {
        #region Members

        /// <inheritdoc />
        public HttpClient CreateClient(Uri listenerUri)
        {
            ArgumentNullException.ThrowIfNull(listenerUri);

            return new HttpClient
            {
                BaseAddress = new Uri(listenerUri.GetLeftPart(UriPartial.Authority))
            };
        }

        #endregion Members
    }
}
