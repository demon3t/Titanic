using Titanic.Common.Services.Authorization.Base;

namespace Titanic.Common.Services.Authorization.Entity
{
	/// <summary>
	/// Обработчик авторизации запроса к сущностям.
	/// </summary>
	public class EntityAuthorizationHandler : BaseHeaderAuthorizationHandler<EntityAuthorizationCollection, EntityAuthorizationRequirement>
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
        public EntityAuthorizationHandler(EntityAuthorizationCollection collection)
			: base(collection)
		{
		}
	}
}
