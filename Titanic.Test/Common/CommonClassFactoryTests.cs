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
            var services = new ServiceCollection();
            services.AddSingleton<TestDependency>();

            using var provider = services.BuildServiceProvider();
            ClassFactory.Reset();
            ClassFactory.Configure(provider);
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
            var services = new ServiceCollection();
            services.AddSingleton<TestDependency>();

            using var provider = services.BuildServiceProvider();
            ClassFactory.Reset();
            ClassFactory.Configure(provider);
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
            var services = new ServiceCollection();
            services.AddSingleton<TestDependency>();

            using var provider = services.BuildServiceProvider();
            ClassFactory.Reset();
            ClassFactory.Configure(provider);

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
            var services = new ServiceCollection();

            using var provider = services.BuildServiceProvider();
            ClassFactory.Reset();
            ClassFactory.Configure(provider);

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
            var services = new ServiceCollection();
            services.AddSingleton<TestDependency>();

            using var provider = services.BuildServiceProvider();
            ClassFactory.Reset();
            ClassFactory.Configure(provider);
            ClassFactory.Bind<ITestListener, TestListener>();
            ClassFactory.RebindWithFactoryMethod<ITestListener>(() => new TestListener("manual", new TestDependency()));

            var listener = ClassFactory.Get<ITestListener>();

            Assert.Equal("manual", listener.EntityName);
        }

        /// <summary>
        /// Инициализирует новый экземпляр ClassFactory_ShouldThrow_WhenConstructorArgumentNameDoesNotMatch.
        /// </summary>
        [Fact]
        public void ClassFactory_ShouldThrow_WhenConstructorArgumentNameDoesNotMatch()
        {
            var services = new ServiceCollection();
            services.AddSingleton<TestDependency>();

            using var provider = services.BuildServiceProvider();
            ClassFactory.Reset();
            ClassFactory.Configure(provider);
            ClassFactory.Bind<ITestListener, TestListener>();

            var exception = Assert.Throws<InvalidOperationException>(() =>
                ClassFactory.Get<ITestListener>(new ConstructorArgument("wrongName", "employees")));

            Assert.Contains(typeof(TestListener).FullName!, exception.Message);
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
