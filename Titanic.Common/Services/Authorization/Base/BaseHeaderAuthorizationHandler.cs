using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Titanic.Common.Services.Authorization.Interfaces;

namespace Titanic.Common.Services.Authorization.Base
{
	/// <summary>
	/// Базовый обработчик автороизации по заголовку.
	/// </summary>
	public abstract class BaseHeaderAuthorizationHandler<TCollection, IRequirement> : AuthorizationHandler<IRequirement> 
		where TCollection : IAuthorizationCollection
		where IRequirement : IAuthorizationRequirement
	{
		#region Свойства

		/// <summary>
		/// Коллекция авторизаций.
		/// </summary>
		protected TCollection Collection { get; private set; }

		/// <summary>
		/// Заголовок авторизации.
		/// </summary>
		protected abstract string AuthorizationHeader { get; }

		#endregion Свойства

		/// <summary>
		/// Конструктор с параметрами.
		/// </summary>
		/// <param name="collection"> Колекиция авторизаций. </param>
		public BaseHeaderAuthorizationHandler(TCollection collection)
		{
			Collection = collection;
		}

		/// <summary>
		/// ПРослойка с авторизацией.
		/// </summary>
		/// <param name="context"> Контекст запроса. </param>
		/// <param name="requirement"> Контекст авторизации. </param>
		protected override Task HandleRequirementAsync(
			AuthorizationHandlerContext context,
			IRequirement requirement)
		{
			if (context.Resource is HttpContext httpContext)
			{
				if (httpContext.Request.Headers.TryGetValue(AuthorizationHeader, out var key))
				{
					if (Collection.CheckAuthorization(key!))
					{
						context.Succeed(requirement);

						return Task.CompletedTask;
					}
				}

				httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
			}

			context.Fail();

			return Task.CompletedTask;
		}
	}
}
