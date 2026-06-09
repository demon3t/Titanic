using System.Net.Http.Json;
using Titanic.Entity.Interfaces;

namespace Titanic.Entity.Events
{
    /// <summary>
    /// Provider HTTP-вызова внешнего обработчика событий Entity ORM.
    /// </summary>
    public sealed class HttpEntityEventProvider : BaseEntityEventProvider
    {
        #region Members

        /// <inheritdoc />
        public override bool CanDispatch(BaseEntityManager manager)
        {
            ArgumentNullException.ThrowIfNull(manager);

            return TryGetListenerUri(manager, out var uri)
                && uri != null
                && (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                    || uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
        }

        /// <inheritdoc />
        public override void Dispatch(
            global::Titanic.Entity.Orm.Entity entity,
            BaseEntityManager manager,
            EntityEventStage stage,
            IServiceProvider services)
        {
            ArgumentNullException.ThrowIfNull(entity);
            ArgumentNullException.ThrowIfNull(manager);
            ArgumentNullException.ThrowIfNull(services);

            var request = CreateRequest(entity, manager, stage);
            var baseUri = ResolveHttpBaseUri(GetListenerUri(manager));
            using var client = CreateClient(baseUri, services);

            if (EntityEventListenerApiDefaults.IsInitialStage(stage))
            {
                SendHttp(request, BuildActionUri(baseUri, EntityEventListenerApiDefaults.HttpCreateActionPath), client);
            }

            try
            {
                var response = SendHttp(
                    request,
                    BuildActionUri(baseUri, EntityEventListenerApiDefaults.GetHttpActionPath(stage)),
                    client);
                ApplyResponseValues(entity, response);
            }
            catch
            {
                TryDeleteRemoteListener(request, baseUri, client);
                throw;
            }

            if (EntityEventListenerApiDefaults.IsFinalStage(stage))
            {
                TryDeleteRemoteListener(request, baseUri, client);
            }
        }

        /// <summary>
        /// Создаёт HTTP-клиент для вызова внешнего listener-а.
        /// </summary>
        /// <param name="baseUri">Базовый URI listener-а.</param>
        /// <param name="services">Провайдер сервисов приложения.</param>
        /// <returns>HTTP-клиент listener-а.</returns>
        private static HttpClient CreateClient(Uri baseUri, IServiceProvider services)
        {
            var factory = services.GetService(typeof(IEntityEventHttpClientFactory)) as IEntityEventHttpClientFactory
                ?? new DefaultEntityEventHttpClientFactory();
            return factory.CreateClient(baseUri);
        }

        /// <summary>
        /// Отправляет запрос во внешний HTTP listener.
        /// </summary>
        /// <param name="request">Transport-запрос события.</param>
        /// <param name="actionUri">URI HTTP-действия.</param>
        /// <param name="client">HTTP-клиент listener-а.</param>
        /// <returns>Ответ событийного listener-а.</returns>
        private static EntityEventDispatchResponse SendHttp(
            EntityEventDispatchRequest request,
            Uri actionUri,
            HttpClient client)
        {
            using var response = client.PostAsJsonAsync(actionUri, request).GetAwaiter().GetResult();

            var body = response.Content.ReadFromJsonAsync<EntityEventDispatchResponse>().GetAwaiter().GetResult();
            body ??= new EntityEventDispatchResponse
            {
                Success = response.IsSuccessStatusCode,
                ErrorMessage = response.ReasonPhrase
            };

            EnsureResponseSuccess(body, response.StatusCode);
            return body;
        }

        /// <summary>
        /// Пытается удалить remote listener, не перекрывая исходный результат обработки события.
        /// </summary>
        /// <param name="request">Transport-запрос события.</param>
        /// <param name="baseUri">Базовый URI listener-а.</param>
        /// <param name="client">HTTP-клиент listener-а.</param>
        private static void TryDeleteRemoteListener(EntityEventDispatchRequest request, Uri baseUri, HttpClient client)
        {
            try
            {
                SendHttp(request, BuildActionUri(baseUri, EntityEventListenerApiDefaults.HttpDeleteActionPath), client);
            }
            catch
            {
                // TTL на стороне listener API удалит экземпляр, если явная очистка не дошла.
            }
        }

        /// <summary>
        /// Возвращает базовый URI HTTP listener-а.
        /// </summary>
        /// <param name="uri">URI listener-а из конфигурации менеджера.</param>
        /// <returns>Базовый URI HTTP listener-а.</returns>
        private static Uri ResolveHttpBaseUri(Uri uri)
        {
            var builder = new UriBuilder(uri)
            {
                Query = string.Empty,
                Fragment = string.Empty
            };

            if (string.IsNullOrWhiteSpace(builder.Path) || builder.Path == "/")
            {
                builder.Path = EntityEventListenerApiDefaults.HttpBasePath.TrimStart('/');
            }

            return new Uri(builder.Uri.AbsoluteUri.TrimEnd('/') + "/");
        }

        /// <summary>
        /// Строит URI конкретного HTTP-действия listener-а.
        /// </summary>
        /// <param name="baseUri">Базовый URI listener-а.</param>
        /// <param name="actionPath">Относительный путь действия.</param>
        /// <returns>URI HTTP-действия.</returns>
        private static Uri BuildActionUri(Uri baseUri, string actionPath)
        {
            return new Uri(baseUri, actionPath);
        }

        #endregion Members
    }
}
