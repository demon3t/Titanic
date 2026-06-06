using Titanic.Common.Services.Authorization.Base;

namespace Titanic.Common.Services.Authorization.Entity
{
    /// <summary>
    /// Мок авторизация Entity.
    /// </summary>
    public class MockEntityAuthorizationHandler : BaseMockHeaderAuthorizationHandler<EntityAuthorizationCollection, EntityAuthorizationRequirement>
    {
        #region Свойства

        /// <summary>
        /// Заголовок авторизации.
        /// </summary>
        protected override string AuthorizationHeader => "X-Entity-Key";

        #endregion Свойства

        /// <summary>
        /// Конструктор с параметрами.
        /// </summary>
        /// <param name="collection"> Коллекция авторизаций. </param>
        public MockEntityAuthorizationHandler(EntityAuthorizationCollection collection)
            : base(collection)
        {
        }
    }
}
