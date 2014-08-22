/*
Weave Server.NET <http://code.google.com/p/weave-server-dotnet/>
Copyright (C) 2014 Karoly Lukacs

Based on code created by Mozilla Labs.

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Configuration;
using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Security;
using Weave.Core;
using Weave.Core.Models;

namespace Weave.Server.Admin {
	public class UserMapper : IUserMapper {
		private WeaveAdmin _weaveAdmin;
		private Guid _adminGuid = new Guid("8907C5E3-29AA-4F2C-90E4-5F62EE95C68B");
		WeaveConfigurationSection _config = (WeaveConfigurationSection)ConfigurationManager.GetSection("weave");

		public UserMapper(WeaveAdmin weaveAdmin) {
			_weaveAdmin = weaveAdmin;
		}

		public IUserIdentity GetUserFromIdentifier(Guid identifier, NancyContext context) {
			if (identifier == _adminGuid) {
				return new User { UserName = "admin", Claims = new List<string> { "Admin" } };
			}

			var user = _weaveAdmin.GetUser(Guid2Long(identifier));

			if (user == null) {
				return null;
			}

			return new User { UserName = user.UserName, UserId = user.UserId, Claims = new List<string> { "User" } };
		}

		public Guid? ValidateUser(string username, string password) {
			if (_config.EnableAdminService && username == _config.AdminLogin) {
				if (password != _config.AdminPassword) {
					return null;
				}

				return _adminGuid;
			}

			long id = _weaveAdmin.AuthenticateUser(username, password);

			if (id == 0) {
				return null;
			}

			return Long2Guid(id);
		}

		public Guid Long2Guid(long value) {
			byte[] bytes = new byte[16];
			BitConverter.GetBytes(value).CopyTo(bytes, 0);

			return new Guid(bytes);
		}

		public long Guid2Long(Guid value) {
			byte[] b = value.ToByteArray();

			return BitConverter.ToInt64(b, 0);
		}
	}
}