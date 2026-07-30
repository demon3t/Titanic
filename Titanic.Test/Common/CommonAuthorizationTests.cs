using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Titanic.Common.Services.Authorization.Entity;
using Titanic.Common.Session;
using Titanic.Common.WebApplication;

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
            Assert.Equal(string.Empty, connection.Culture.Name);
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

        [Fact]
        public void EntityAuthorizationCollection_CheckAuthorization_ShouldCacheExternalAuthorization()
        {
            var timeProvider = new AdjustableTimeProvider(DateTimeOffset.Parse("2026-06-06T10:00:00Z"));
            var connection = CreateConnection();
            var collection = new ExternalEntityAuthorizationCollection(
                timeProvider,
                TimeSpan.FromMinutes(5),
                "external-key",
                connection);

            Assert.True(collection.CheckAuthorization("external-key"));
            Assert.True(collection.CheckAuthorization("external-key"));
            Assert.Equal(1, collection.LookupCount);
        }

        [Fact]
        public void AddHeaderAuthorization_ShouldKeepExistingAuthorizationCollectionRegistration()
        {
            var builder = WebApplication.CreateBuilder();
            var collection = new EntityAuthorizationCollection();
            builder.Services.AddSingleton(collection);

            builder.AddHeaderAuthorization<
                EntityAuthorizationHandler,
                EntityAuthorizationCollection,
                EntityAuthorizationRequirement>("Entity");

            using var app = builder.Build();
            var resolvedCollection = app.Services.GetRequiredService<EntityAuthorizationCollection>();

            Assert.Same(collection, resolvedCollection);
        }

        private static UserConnection CreateConnection()
        {
            return new UserConnection
            {
                UserId = Guid.NewGuid(),
                Culture = new UserCulture
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Name = "ru-RU"
                }
            };
        }

        private sealed class ExternalEntityAuthorizationCollection : EntityAuthorizationCollection
        {
            private readonly UserConnection _connection;
            private readonly string _key;

            public ExternalEntityAuthorizationCollection(
                TimeProvider timeProvider,
                TimeSpan maxLifetime,
                string key,
                UserConnection connection)
                : base(timeProvider, maxLifetime)
            {
                _key = key;
                _connection = connection;
            }

            public int LookupCount { get; private set; }

            public override bool TryGet(string key, out UserConnection? userConnection)
            {
                LookupCount++;

                if (!string.Equals(key, _key, StringComparison.Ordinal))
                {
                    userConnection = null;
                    return false;
                }

                userConnection = _connection;
                return true;
            }
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
