/*
Weave Server.NET <http://code.google.com/p/weave-server-dotnet/>
Copyright (C) 2013 Karoly Lukacs

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

using System.Collections.Generic;
using System.Dynamic;
using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Security;
using NLog;
using Weave.Core;
using Weave.Core.Models;

namespace Weave.Admin.Server.Modules {
	public class AccountModule : NancyModule {
		private static Logger _logger = LogManager.GetCurrentClassLogger();

		WeaveAdmin _weaveAdmin = new WeaveAdmin();

		public AccountModule() {
			_weaveAdmin.LogEvent += OnLogEvent;

			this.RequiresAuthentication();
			this.RequiresClaims(new[] { "User" });

			Get["/Account/"] = parameters => {
				dynamic model = new ExpandoObject();
				model.IsAuthenticated = true;
				model.UserName = Context.CurrentUser.UserName;

				return View["Account", model];
			};

			Get["/Account/GetUserSummary"] = parameters => {
				var userList = new List<Core.Models.User>();
				userList.Add(_weaveAdmin.GetUser(((User) Context.CurrentUser).UserId));

				return Response.AsJson(userList);
			};

			Get["/Account/GetUserDetails"] = parameters => Response.AsJson(_weaveAdmin.GetUserDetails(((User)Context.CurrentUser).UserId));

			Post["/Account/ChangePassword"] = parameters => {
				string pswd = Request.Form["password"];
				string result = _weaveAdmin.ChangePassword(((User)Context.CurrentUser).UserId, pswd);

				return result;
			};

			Post["/Account/ClearUserData"] = parameters => _weaveAdmin.ClearUserData(((User)Context.CurrentUser).UserId);

			Post["/Account/DeleteUser"] = parameters => _weaveAdmin.DeleteUser(((User)Context.CurrentUser).UserId);

			Get["/Logout"] = parameters => this.LogoutAndRedirect("Login");
		}

		private void OnLogEvent(object source, LogEventArgs args) {
			var level = LogLevel.Off;
			switch (args.Type) {
				case LogType.Error:
					level = LogLevel.Error;
					break;
				case LogType.Info:
					level = LogLevel.Info;
					break;
				case LogType.Warning:
					level = LogLevel.Warn;
					break;
				case LogType.Debug:
					level = LogLevel.Debug;
					break;
			}

			_logger.Log(level, args.Message);
		}
	}
}