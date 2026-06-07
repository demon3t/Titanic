using Titanic.Common.Services.Authorization.Entity;
using Titanic.Common.Session;

namespace Titanic.Test.Common
{
    /// <summary>
    /// Тесты базовой инфраструктуры Titanic.Common.
    /// </summary>
    public class CommonAuthorizationTests
    {
        [Fact]
        public void UserConnection_ShouldCreateDefaultCulture()
        {
            var connection = new UserConnection();

            Assert.NotNull(connection.Culture);
            Assert.False(connection.IsAdmin);
            Assert.Empty(connection.Roles);
            Assert.Equal(string.Empty, connection.Culture.Name);
        }

        [Fact]
        public void UserConnection_IsAdmin_ShouldDependOnRoles()
        {
            var connection = new UserConnection();

            Assert.False(connection.IsAdmin);

            connection.Roles.Add(UserRoles.Administrator);

            Assert.True(connection.IsAdmin);
            Assert.True(connection.HasRole(UserRoles.Administrator));
        }

        [Fact]
        public void EntityAuthorizationCollection_CheckAuthorization_ShouldReturnTrue_ForCachedKey()
        {
            var timeProvider = new AdjustableTimeProvider(DateTimeOffset.Parse("2026-06-06T10:00:00Z"));
            var collection = new EntityAuthorizationCollection(timeProvider, TimeSpan.FromMinutes(5));

            collection.AddAuthorization("key", CreateConnection());

            Assert.True(collection.CheckAuthorization("key"));
        }

        [Fact]
        public void EntityAuthorizationCollection_CheckAuthorization_ShouldRefreshLastAccess()
        {
            var timeProvider = new AdjustableTimeProvider(DateTimeOffset.Parse("2026-06-06T10:00:00Z"));
            var collection = new EntityAuthorizationCollection(timeProvider, TimeSpan.FromMinutes(5));

            collection.AddAuthorization("key", CreateConnection());

            timeProvider.Advance(TimeSpan.FromMinutes(4));
            Assert.True(collection.CheckAuthorization("key"));

            timeProvider.Advance(TimeSpan.FromMinutes(4));
            Assert.True(collection.CheckAuthorization("key"));
        }

        [Fact]
        public void EntityAuthorizationCollection_CheckAuthorization_ShouldReturnFalse_ForExpiredKey()
        {
            var timeProvider = new AdjustableTimeProvider(DateTimeOffset.Parse("2026-06-06T10:00:00Z"));
            var collection = new EntityAuthorizationCollection(timeProvider, TimeSpan.FromMinutes(5));

            collection.AddAuthorization("key", CreateConnection());
            timeProvider.Advance(TimeSpan.FromMinutes(6));

            Assert.False(collection.CheckAuthorization("key"));
        }

        private static UserConnection CreateConnection()
        {
            return new UserConnection
            {
                UserId = Guid.NewGuid(),
                ContactId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                TimeZoneId = "Europe/Moscow",
                Culture = new UserCulture
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Name = "ru-RU"
                }
            };
        }

        private sealed class AdjustableTimeProvider : TimeProvider
        {
            private DateTimeOffset _utcNow;

            public AdjustableTimeProvider(DateTimeOffset utcNow)
            {
                _utcNow = utcNow;
            }

            public override DateTimeOffset GetUtcNow() => _utcNow;

            public void Advance(TimeSpan value)
            {
                _utcNow = _utcNow.Add(value);
            }
        }
    }
}
