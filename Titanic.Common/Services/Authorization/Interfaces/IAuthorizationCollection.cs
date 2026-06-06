using Titanic.Common.Session;

namespace Titanic.Common.Services.Authorization.Interfaces
{
	/// <summary>
	/// Коллекция авторизованных пользователей.
	/// </summary>
	public interface IAuthorizationCollection
	{
		/// <summary>
		/// Добавить авторизацию пользователя.
		/// </summary>
		/// <param name="key"> Ключ. </param>
		/// <param name="userConnection"> Контест пользователя. </param>
		void AddAuthorization(string key, UserConnection userConnection);

		/// <summary>
		/// Удалить авторизацию.
		/// </summary>
		/// <param name="key"> Ключ авторизации. </param>
		void RemoveAuthorization(string key);

		/// <summary>
		/// Проверить авторизацию.
		/// </summary>
		/// <param name="key"> Ключ авторизации. </param>
		bool CheckAuthorization(string key);

		/// <summary>
		/// Попытаться получить авторизацию из внешних систем.
		/// </summary>
		/// <param name="key"> Ключ авторизации. </param>
		/// <param name="userConnection"> Контест пользователя. </param>
		bool TryGet(string key, out UserConnection? userConnection);
	}
}
