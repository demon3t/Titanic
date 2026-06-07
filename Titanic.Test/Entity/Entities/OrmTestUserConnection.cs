using Titanic.Common.Session;

namespace Titanic.Test.Entity;

internal static class OrmTestUserConnection
{
    public static UserConnection Create(Guid? cultureId = null, bool isAdmin = false)
    {
        var roles = isAdmin
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Admin" }
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return new UserConnection
        {
            UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Roles = roles,
            Culture = new UserCulture
            {
                Id = cultureId ?? Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                Name = "Test"
            }
        };
    }
}
