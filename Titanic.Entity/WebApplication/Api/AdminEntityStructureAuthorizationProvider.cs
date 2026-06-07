using Microsoft.AspNetCore.Http;
using Titanic.Entity.Interfaces;

namespace Titanic.Entity.WebApplication.Api
{
    /// <summary>
    /// Проверяет доступ к структуре менеджера через обычную авторизацию Entity API
    /// и дополнительно требует признак администратора в <see cref="Common.Session.UserConnection" />.
    /// </summary>
    internal sealed class AdminEntityStructureAuthorizationProvider : IEntityApiAuthorizationProvider
    {
        private readonly EntityApiAuthorizationProviderFactory _factory;

        /// <summary>
        /// Инициализировать провайдер.
        /// </summary>
        public AdminEntityStructureAuthorizationProvider(EntityApiAuthorizationProviderFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <inheritdoc />
        public async ValueTask<EntityApiAuthorizationResult> AuthorizeAsync(HttpContext context, BaseEntityManager manager)
        {
            var apiAuthorizationProvider = _factory.CreateProvider(context.RequestServices, manager, EntityApiAuthorizationProviderKind.Default);
            var authorization = await apiAuthorizationProvider.AuthorizeAsync(context, manager);
            if (!authorization.IsAuthorized || authorization.UserConnection == null)
            {
                return authorization;
            }

            return authorization.UserConnection.IsAdmin
                ? authorization
                : EntityApiAuthorizationResult.Fail("Administrator permissions are required.");
        }
    }
}
