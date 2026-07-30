using Microsoft.Extensions.DependencyInjection;
using Titanic.Common.Services.Factory;

namespace Titanic.Test.Common
{
    /// <summary>
    /// Тесты фабрики классов Titanic.Common.
    /// </summary>
    public sealed class CommonClassFactoryTests
    {
        #region Members

        /// <summary>
        /// Инициализирует новый экземпляр ClassFactory_ShouldCreateRegisteredClass_WithInjectedDependencyAndNamedConstructorArgument.
        /// </summary>
        [Fact]
        public void ClassFactory_ShouldCreateRegisteredClass_WithInjectedDependencyAndNamedConstructorArgument()
        {
            using var provider = ConfigureFactory(services => services.AddSingleton<TestDependency>());
            ClassFactory.Bind<ITestListener, TestListener>();

            var listener = ClassFactory.Get<ITestListener>(new ConstructorArgument("entityName", "employees"));

            Assert.Equal("employees", listener.EntityName);
            Assert.NotNull(listener.Dependency);
        }

        /// <summary>
        /// Инициализирует новый экземпляр ClassFactory_ShouldResolveNamedBinding.
        /// </summary>
        [Fact]
        public void ClassFactory_ShouldResolveNamedBinding()
        {
            using var provider = ConfigureFactory(services => services.AddSingleton<TestDependency>());
            ClassFactory.Bind<ITestListener, TestListener>("employees");

            var listener = ClassFactory.Get<ITestListener>("employees");

            Assert.Equal("default", listener.EntityName);
        }

        /// <summary>
        /// Инициализирует новый экземпляр ClassFactory_ShouldForceGetClassByFullName.
        /// </summary>
        [Fact]
        public void ClassFactory_ShouldForceGetClassByFullName()
        {
            using var provider = ConfigureFactory(services => services.AddSingleton<TestDependency>());

            var listener = ClassFactory.ForceGet<ITestListener>(
                typeof(TestListener).FullName!,
                new ConstructorArgument("entityName", "employees"));

            Assert.Equal("employees", listener.EntityName);
        }

        /// <summary>
        /// Инициализирует новый экземпляр ClassFactory_ShouldReturnFalse_WhenNamedBindingDoesNotExist.
        /// </summary>
        [Fact]
        public void ClassFactory_ShouldReturnFalse_WhenNamedBindingDoesNotExist()
        {
            using var provider = ConfigureFactory();

            var found = ClassFactory.TryGet<ITestListener>("missing", out var listener);

            Assert.False(found);
            Assert.Null(listener);
        }

        /// <summary>
        /// Инициализирует новый экземпляр ClassFactory_ShouldSupportRebindWithFactoryMethod.
        /// </summary>
        [Fact]
        public void ClassFactory_ShouldSupportRebindWithFactoryMethod()
        {
            using var provider = ConfigureFactory(services => services.AddSingleton<TestDependency>());
            ClassFactory.Bind<ITestListener, TestListener>();
            ClassFactory.RebindWithFactoryMethod<ITestListener>(() => new TestListener("manual", new TestDependency()));

            var listener = ClassFactory.Get<ITestListener>();

            Assert.Equal("manual", listener.EntityName);
        }

        [Fact]
        public void ClassFactory_ShouldResolveFactoryMethod_WithoutConfiguredServiceProvider()
        {
            ClassFactory.Reset();
            ClassFactory.Bind<ITestListener>(() => new TestListener("manual", new TestDependency()));

            var listener = ClassFactory.Get<ITestListener>();

            Assert.Equal("manual", listener.EntityName);
            Assert.NotNull(listener.Dependency);
        }

        /// <summary>
        /// Инициализирует новый экземпляр ClassFactory_ShouldThrow_WhenConstructorArgumentNameDoesNotMatch.
        /// </summary>
        [Fact]
        public void ClassFactory_ShouldThrow_WhenConstructorArgumentNameDoesNotMatch()
        {
            using var provider = ConfigureFactory(services => services.AddSingleton<TestDependency>());
            ClassFactory.Bind<ITestListener, TestListener>();

            var exception = Assert.Throws<InvalidOperationException>(() =>
                ClassFactory.Get<ITestListener>(new ConstructorArgument("wrongName", "employees")));

            Assert.Contains(typeof(TestListener).FullName!, exception.Message);
        }

        private static ServiceProvider ConfigureFactory(Action<IServiceCollection>? configure = null)
        {
            var services = new ServiceCollection();
            configure?.Invoke(services);

            var provider = services.BuildServiceProvider();
            ClassFactory.Reset();
            ClassFactory.Configure(provider);
            return provider;
        }

        private sealed class TestDependency
        {
        }

        private interface ITestListener
        {
            string EntityName { get; }

            TestDependency Dependency { get; }
        }

        private sealed class TestListener : ITestListener
        {
            /// <summary>
            /// Инициализирует новый экземпляр TestListener.
            /// </summary>
            public TestListener(TestDependency dependency)
                : this("default", dependency)
            {
            }

            /// <summary>
            /// Инициализирует новый экземпляр TestListener.
            /// </summary>
            public TestListener(string entityName, TestDependency dependency)
            {
                EntityName = entityName;
                Dependency = dependency;
            }

            public string EntityName { get; }

            public TestDependency Dependency { get; }
        }

        #endregion Members
    }
}
