using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using Titanic.Common.Services.Authorization.Interfaces;

namespace Titanic.Common.Services.Authorization.Base
{
    /// <summary>
    /// Базовый мок-обработчик авторизации по заголовку.
    /// </summary>
    public abstract class BaseMockHeaderAuthorizationHandler<TCollection, TRequirement>
        : BaseHeaderAuthorizationHandler<TCollection, TRequirement>
        where TCollection : IAuthorizationCollection
        where TRequirement : IAuthorizationRequirement
    {
        /// <summary>
        /// Префикс заголовка.
        /// </summary>
        private const string Prefix = "Mock_";

        /// <summary>
        /// Разрешённая разница времени.
        /// </summary>
        private static readonly TimeSpan AllowedDrift = TimeSpan.FromSeconds(100000000);

        /// <summary>
        /// Конструктор с параметрами.
        /// </summary>
        /// <param name="collection">Коллекция авторизаций.</param>
        protected BaseMockHeaderAuthorizationHandler(TCollection collection)
            : base(collection)
        {
        }

        /// <summary>
        /// Обработать авторизацию mock-заголовка.
        /// </summary>
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, TRequirement requirement)
        {
            if (context.Resource is HttpContext httpContext
                && httpContext.Request.Headers.TryGetValue(AuthorizationHeader, out var key)
                && IsValidMockHeader(key.ToString()))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            context.Fail();
            return Task.CompletedTask;
        }

        private static bool IsValidMockHeader(string header)
        {
            if (!header.StartsWith(Prefix, StringComparison.Ordinal))
            {
                return false;
            }

            var datePart = header[Prefix.Length..];
            if (!DateTime.TryParse(
                datePart,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsedTime))
            {
                return false;
            }

            return Math.Abs((DateTime.UtcNow - parsedTime).TotalSeconds) <= AllowedDrift.TotalSeconds;
        }
    }
}
