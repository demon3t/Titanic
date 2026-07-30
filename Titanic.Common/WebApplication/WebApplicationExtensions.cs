using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Titanic.Common.Services.Authentication;
using Titanic.Common.Services.Authorization.Base;
using Titanic.Common.Services.Authorization.Interfaces;

namespace Titanic.Common.WebApplication
{
    /// <summary>
    /// Методы регистрации общей web-инфраструктуры Titanic.
    /// </summary>
    public static class WebApplicationExtensions
    {
        /// <summary>
        /// Добавить авторизацию по заголовку.
        /// </summary>
        /// <param name="policyName">Название политики.</param>
        public static WebApplicationBuilder AddHeaderAuthorization<THandler, TCollection, TRequirement>(
            this WebApplicationBuilder builder,
            string policyName)
            where THandler : BaseHeaderAuthorizationHandler<TCollection, TRequirement>
            where TCollection : class, IAuthorizationCollection
            where TRequirement : class, IAuthorizationRequirement, new()
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentException.ThrowIfNullOrWhiteSpace(policyName);

            builder.Services.TryAddSingleton<TCollection>();
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuthorizationHandler, THandler>());

            builder.Services.AddAuthorizationBuilder()
                .AddPolicy(policyName, policy =>
                {
                    policy.Requirements.Add(new TRequirement());
                });

            builder.Services.AddHttpContextAccessor();
            return builder;
        }

        /// <summary>
        /// Добавить пустую аутентификацию.
        /// </summary>
        public static WebApplicationBuilder AddEmptyAuthentication(this WebApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services.AddAuthentication("EmptyScheme")
                .AddScheme<AuthenticationSchemeOptions, EmptyAuthenticationHandler>("EmptyScheme", _ => { });

            return builder;
        }

        /// <summary>
        /// Добавить Swagger и endpoint API explorer.
        /// </summary>
        public static WebApplicationBuilder AddTitanicInfrastructure(this WebApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            return builder;
        }
    }
}
