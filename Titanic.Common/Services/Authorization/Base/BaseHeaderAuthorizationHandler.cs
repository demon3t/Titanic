using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Titanic.Common.Services.Authorization.Interfaces;

namespace Titanic.Common.Services.Authorization.Base
{
    /// <summary>
    /// Базовый обработчик авторизации по HTTP-заголовку.
    /// </summary>
    public abstract class BaseHeaderAuthorizationHandler<TCollection, TRequirement> : AuthorizationHandler<TRequirement>
        where TCollection : IAuthorizationCollection
        where TRequirement : IAuthorizationRequirement
    {
        #region Properties

        /// <summary>
        /// Коллекция авторизаций.
        /// </summary>
        protected TCollection Collection { get; }

        /// <summary>
        /// Заголовок авторизации.
        /// </summary>
        protected abstract string AuthorizationHeader { get; }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Конструктор с параметрами.
        /// </summary>
        /// <param name="collection">Коллекция авторизаций.</param>
        protected BaseHeaderAuthorizationHandler(TCollection collection)
        {
            Collection = collection ?? throw new ArgumentNullException(nameof(collection));
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Обработать авторизацию по заголовку.
        /// </summary>
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            TRequirement requirement)
        {
            if (context.Resource is not HttpContext httpContext)
            {
                context.Fail();
                return Task.CompletedTask;
            }

            if (httpContext.Request.Headers.TryGetValue(AuthorizationHeader, out var key)
                && Collection.CheckAuthorization(key.ToString()))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Fail();
            return Task.CompletedTask;
        }

        #endregion Methods
    }
}
