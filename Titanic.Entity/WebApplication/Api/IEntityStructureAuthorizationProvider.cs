using Microsoft.AspNetCore.Http;
using Titanic.Entity.Interfaces;

namespace Titanic.Entity.WebApplication.Api
{
    /// <summary>
    /// Провайдер проверки доступа к endpoint-у структуры Entity ORM менеджера.
    /// </summary>
    public interface IEntityStructureAuthorizationProvider
    {
        /// <summary>
        /// Проверить доступ к структуре менеджера и вернуть контекст пользователя.
        /// </summary>
        /// <param name="context"> HTTP-контекст. </param>
        /// <param name="manager"> Entity ORM менеджер. </param>
        /// <returns> Результат проверки авторизации. </returns>
        ValueTask<EntityApiAuthorizationResult> AuthorizeAsync(HttpContext context, BaseEntityManager manager);
    }
}
