using Titanic.Common.Session;

namespace Titanic.Test.Entity;

internal static class OrmTestUserConnection
{
    public static UserConnection Create(Guid? cultureId = null)
    {
        var userId = Guid.NewGuid();

        return new UserConnection
        {
            UserId = userId,
            Culture = new UserCulture
            {
                Id = cultureId ?? Guid.NewGuid(),
                Name = "Test"
            }
        };
    }
}
