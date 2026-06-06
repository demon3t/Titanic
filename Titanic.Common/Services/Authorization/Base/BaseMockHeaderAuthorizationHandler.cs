using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using Titanic.Common.Services.Authorization.Interfaces;

namespace Titanic.Common.Services.Authorization.Base
{
	/// <summary>
	/// Базовый мок обработчик автороизации по заголовку.
	/// </summary>
	public abstract class BaseMockHeaderAuthorizationHandler<TCollection, IRequirement> : BaseHeaderAuthorizationHandler<TCollection, IRequirement>
        where TCollection : IAuthorizationCollection
        where IRequirement : IAuthorizationRequirement
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
        /// <param name="collection"> Колекиция авторизаций. </param>
        public BaseMockHeaderAuthorizationHandler(TCollection collection)
			: base(collection)
		{

		}

        /// <summary>
        /// Прослойка с авторизацией.
        /// </summary>
        /// <param name="context"> Контекст запроса. </param>
        /// <param name="requirement"> Контекст авторизации. </param>
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, IRequirement requirement)
        {
            if (context.Resource is HttpContext httpContext)
            {
                if (httpContext.Request.Headers.TryGetValue(AuthorizationHeader, out var key))
                {
                    var headerValue = key.ToString();

                    if (IsValidMockHeader(headerValue))
                    {
                        context.Succeed(requirement);
                        return Task.CompletedTask;
                    }
                }
            }

            context.Fail();
            return Task.CompletedTask;
        }

        private bool IsValidMockHeader(string header)
        {
            if (!header.StartsWith(Prefix))
            {
                return false;
            }

            var datePart = header.Substring(Prefix.Length);

            if (!DateTime.TryParse(datePart, CultureInfo.InvariantCulture,  DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsedTime))
            {
                return false;
            }

            var now = DateTime.UtcNow;

            var r1 = Math.Abs((now - parsedTime).TotalSeconds);
            var r2 = AllowedDrift.TotalSeconds;

            return r1 <= r2;
        }
    }
}
