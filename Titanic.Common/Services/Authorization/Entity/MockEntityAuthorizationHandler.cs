using Titanic.Common.Services.Authorization.Base;

namespace Titanic.Common.Services.Authorization.Entity
{
    /// <summary>
    /// Мок-обработчик авторизации Entity.
    /// </summary>
    public sealed class MockEntityAuthorizationHandler : BaseMockHeaderAuthorizationHandler<EntityAuthorizationCollection, EntityAuthorizationRequirement>
    {
        #region Properties

        /// <summary>
        /// Заголовок авторизации.
        /// </summary>
        protected override string AuthorizationHeader => "X-Entity-Key";

        #endregion Properties

        /// <summary>
        /// Конструктор с параметрами.
        /// </summary>
        /// <param name="collection">Коллекция авторизаций.</param>
        public MockEntityAuthorizationHandler(EntityAuthorizationCollection collection)
            : base(collection)
        {
        }
    }
}
