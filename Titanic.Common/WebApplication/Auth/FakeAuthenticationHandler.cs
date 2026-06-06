using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Titanic.Common.WebApplication.Auth
{
	internal class FakeAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
	{
		public FakeAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
			ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
			: base(options, logger, encoder, clock) { }

		protected override Task<AuthenticateResult> HandleAuthenticateAsync()
		{
			// Создаём фиктивного пользователя с ролью Admin
			var claims = new[] { new Claim(ClaimTypes.Name, "EntityRole"), new Claim(ClaimTypes.Role, "Admin") };
			var identity = new ClaimsIdentity(claims, Scheme.Name);
			var principal = new ClaimsPrincipal(identity);
			var ticket = new AuthenticationTicket(principal, Scheme.Name);

			return Task.FromResult(AuthenticateResult.Success(ticket));
		}
	}
}
