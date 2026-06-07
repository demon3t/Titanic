namespace Titanic.Common.Session
{
    public class UserConnection
    {
        public Guid UserId { get; set; }

        public HashSet<string> Roles { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public UserCulture Culture { get; set; } = new();
    }
}
