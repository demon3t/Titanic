using Titanic.Common.Session;

namespace Titanic.Test.Entity;

internal static class OrmTestUserConnection
{
    public static UserConnection Create(Guid? cultureId = null, bool isAdmin = false)
    {
        return new UserConnection
        {
            UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            IsAdmin = isAdmin,
            Culture = new UserCulture
            {
                Id = cultureId ?? Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                Name = "Test"
            }
        };
    }
}
