using Microsoft.AspNetCore.Http;
using Titanic.Entity.Interfaces;

namespace Titanic.Entity.WebApplication.Api
{
    /// <summary>
    /// Провайдер проверки авторизации Entity API.
    /// </summary>
    public interface IEntityApiAuthorizationProvider
    {
        /// <summary>
        /// Проверить авторизацию текущего HTTP-запроса и получить UserConnection.
        /// </summary>
        /// <param name="context"> HTTP-контекст. </param>
        /// <param name="manager"> Entity ORM менеджер. </param>
        /// <returns> Результат проверки авторизации. </returns>
        ValueTask<EntityApiAuthorizationResult> AuthorizeAsync(HttpContext context, BaseEntityManager manager);
    }
}
