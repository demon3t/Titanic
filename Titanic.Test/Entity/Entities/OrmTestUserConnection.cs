using Titanic.Common.Session;

namespace Titanic.Test.Entity;

internal static class OrmTestUserConnection
{
    public static UserConnection Create(Guid? cultureId = null, bool isAdmin = false)
    {
        var userId = Guid.NewGuid();
        var roles = isAdmin
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Admin" }
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return new UserConnection
        {
            UserId = userId,
            Roles = roles,
            Culture = new UserCulture
            {
                Id = cultureId ?? Guid.NewGuid(),
                Name = "Test"
            }
        };
    }
}
