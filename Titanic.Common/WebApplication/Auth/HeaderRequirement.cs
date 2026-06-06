using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Titanic.Common.WebApplication.Auth
{
	internal class HeaderRequirement : IAuthorizationRequirement
	{
		public string HeaderName { get; }
		public string ExpectedValue { get; }

		public HeaderRequirement(string headerName, string expectedValue)
		{
			HeaderName = headerName;
			ExpectedValue = expectedValue;
		}
	}
}
