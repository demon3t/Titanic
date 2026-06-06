using Microsoft.AspNetCore.Authorization;

namespace Titanic.Common.Services.Authorization.Entity
{
    /// <summary>
    /// Требование авторизации для доступа к Entity endpoint-ам.
    /// </summary>
    public sealed class EntityAuthorizationRequirement : IAuthorizationRequirement
    {
    }
}
