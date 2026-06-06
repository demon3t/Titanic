using Titanic.Common.Services.Authorization.Interfaces;
using Titanic.Common.Session;

namespace Titanic.Common.Services.Authorization.Entity
{
	/// <summary>
	/// Коллекция авторизаций для запросов к сущностям.
	/// </summary>
	public class EntityAuthorizationCollection : IAuthorizationCollection
	{
		#region Свойства

		/// <summary>
		/// Коллекция авторизаций.
		/// </summary>
		protected Dictionary<string, (DateTime LastAppeal, UserConnection Connection)> AuthorizationCollection { get; set; }

		/// <summary>
		/// Время жизни авторизации в секундах.
		/// </summary>
		protected int MaxTimeLiveInSeconds { get; private set; } = 86400;

		#endregion Свойства

		#region Конструкторы

		/// <summary>
		/// Конструктор без параметров.
		/// </summary>
		public EntityAuthorizationCollection()
		{
			AuthorizationCollection = new Dictionary<string, (DateTime LastAppeal, UserConnection Connection)>();
		}

		#endregion Конструкторы

		#region Методы public

		/// <summary>
		/// Добавить авторизацию.
		/// </summary>
		/// <param name="key"> Ключ авторизации. </param>
		/// <param name="userConnection"> Контекст авторизации. </param>
		public void AddAuthorization(string key, UserConnection userConnection)
		{
			AuthorizationCollection[key] = (DateTime.UtcNow, userConnection);
		}

		/// <summary>
		/// Удалить авторизацию.
		/// </summary>
		/// <param name="key"> Ключ авторизации. </param>
		public void RemoveAuthorization(string key)
		{
			if (AuthorizationCollection.TryGetValue(key, out var existUserConnnection))
			{
				AuthorizationCollection.Remove(key);
			}
		}

		/// <summary>
		/// Проверить авторизацию.
		/// </summary>
		/// <param name="key"> Ключ авторизации. </param>
		public bool CheckAuthorization(string key)
		{
			if (AuthorizationCollection.TryGetValue(key, out var value))
			{
				return value.LastAppeal <= DateTime.UtcNow.AddSeconds(MaxTimeLiveInSeconds);
			}

			if (TryGet(key, out var existUserConnnection))
			{
				AddAuthorization(key, existUserConnnection!);

				return existUserConnnection is not null;
			}

			return false;
		}

		public bool TryGet(string key, out UserConnection? userConnection)
		{
			userConnection = null;

			// TODO сделать обращение к общему сервису.
			if (Random.Shared.NextDouble() > 0.5)
			{
				userConnection = new UserConnection()
				{
					UserId = Guid.NewGuid(),
					Culture = new UserCulture()
					{
						Id = Guid.NewGuid(),
						Name = "ru-RU"
					}
				};
			}

			return userConnection is not null;
		}

		#endregion Методы public
	}
}
