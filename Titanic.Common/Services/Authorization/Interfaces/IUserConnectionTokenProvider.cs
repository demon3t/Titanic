using Microsoft.AspNetCore.Http;
using Titanic.Common.Session;

namespace Titanic.Common.Services.Authorization.Interfaces
{
    /// <summary>
    /// Контракт поиска пользовательского контекста по токену авторизации.
    /// </summary>
    public interface IUserConnectionTokenProvider
    {
        /// <summary>
        /// Найти контекст пользователя по токену.
        /// </summary>
        /// <param name="token"> Токен авторизации. </param>
        /// <param name="context"> HTTP-контекст запроса. </param>
        /// <returns> Контекст пользователя или <c>null</c>, если токен не авторизован. </returns>
        ValueTask<UserConnection?> FindByTokenAsync(string token, HttpContext context);
    }
}
