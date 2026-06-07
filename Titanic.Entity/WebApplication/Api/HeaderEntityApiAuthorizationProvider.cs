using Microsoft.AspNetCore.Http;
using Titanic.Common.Session;
using Titanic.Entity.Interfaces;

namespace Titanic.Entity.WebApplication.Api
{
    /// <summary>
    /// Базовый провайдер авторизации Entity API по HTTP-заголовку.
    /// Возвращает <see cref="UserConnection" />, который дальше используется всеми операциями API.
    /// </summary>
    public sealed class HeaderEntityApiAuthorizationProvider : IEntityApiAuthorizationProvider
    {
        #region Constants

        private const string CultureHeaderName = "X-Entity-Culture";
        private const string RolesHeaderName = "X-Entity-Roles";

        #endregion Constants

        #region IEntityApiAuthorizationProvider

        /// <inheritdoc />
        public ValueTask<EntityApiAuthorizationResult> AuthorizeAsync(HttpContext context, BaseEntityManager manager)
        {
            if (!context.Request.Headers.TryGetValue(manager.Api.AuthorizationHeaderName, out var key)
                || string.IsNullOrWhiteSpace(key.ToString()))
            {
                return ValueTask.FromResult(EntityApiAuthorizationResult.Fail(
                    $"Header '{manager.Api.AuthorizationHeaderName}' is required."));
            }

            var headerValue = key.ToString();
            var userConnection = new UserConnection
            {
                UserId = Guid.TryParse(headerValue, out var userId) ? userId : CreateDeterministicGuid(headerValue),
                Roles = ResolveRoles(context),
                Culture = ResolveCulture(context)
            };

            return ValueTask.FromResult(EntityApiAuthorizationResult.Success(userConnection));
        }

        #endregion IEntityApiAuthorizationProvider

        #region Private Methods

        /// <summary>
        /// Создать стабильный Guid из строкового ключа.
        /// </summary>
        /// <param name="value"> Строковый ключ. </param>
        /// <returns> Стабильный Guid. </returns>
        private static Guid CreateDeterministicGuid(string value)
        {
            var bytes = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(value));
            return new Guid(bytes);
        }

        /// <summary>
        /// Получить культуру пользователя из результата авторизации.
        /// </summary>
        /// <param name="context"> HTTP-контекст. </param>
        /// <returns> Культура пользователя. </returns>
        private static UserCulture ResolveCulture(HttpContext context)
        {
            var cultureId = context.Request.Headers.TryGetValue(CultureHeaderName, out var cultureHeader)
                && Guid.TryParse(cultureHeader.ToString(), out var headerCultureId)
                    ? headerCultureId
                    : Guid.Empty;

            return new UserCulture
            {
                Id = cultureId,
                Name = "Default"
            };
        }

        /// <summary>
        /// Получить роли пользователя из заголовков запроса.
        /// </summary>
        /// <param name="context"> HTTP-контекст. </param>
        /// <returns> Коллекция ролей пользователя. </returns>
        private static HashSet<string> ResolveRoles(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue(RolesHeaderName, out var headerValue))
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            return headerValue
                .ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        #endregion Private Methods
    }
}
