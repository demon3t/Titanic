using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Titanic.Common.Services.Authentication;
using Titanic.Common.Services.Authorization.Base;
using Titanic.Common.Services.Authorization.Interfaces;

namespace Titanic.Common.WebApplication
{
	public static class WebApplicationExtensions
	{
		/// <summary>
		/// Добавить авторизацию по заголовку.
		/// </summary>
		/// <param name="policyName"> Название политики. </param>
		public static WebApplicationBuilder AddHeaderAuthorization<THandler, TCollection, TRequirement>(this WebApplicationBuilder builder, string policyName)
			where THandler : BaseHeaderAuthorizationHandler<TCollection, TRequirement>
			where TCollection : class, IAuthorizationCollection
			where TRequirement : class, IAuthorizationRequirement, new()
		{
			builder.Services.AddSingleton<TCollection>();

			builder.Services.AddSingleton<IAuthorizationHandler, THandler>();

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
			builder.Services.AddAuthentication("EmptyScheme")
				.AddScheme<AuthenticationSchemeOptions, EmptyAuthenticationHandler>("EmptyScheme", options => { });

			return builder;
		}

		public static WebApplicationBuilder AddTitanicInfrastructure(this WebApplicationBuilder builder)
		{
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen();

            return builder;
		}
    }
}
