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

using System.Dynamic;
using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Security;
using NLog;
using Weave.Core;
using Weave.Core.Models;

namespace Weave.Server.Admin.Modules {
    public class AdminModule : NancyModule {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        WeaveAdmin _weaveAdmin = new WeaveAdmin();

        public AdminModule() {
            _weaveAdmin.LogEvent += OnLogEvent;

            this.RequiresAuthentication();
            this.RequiresClaims(new[] { "Admin" });

            Get["/Admin/"] = parameters => {
                dynamic model = new ExpandoObject();
                model.IsAuthenticated = true;
                model.UserName = Context.CurrentUser.UserName;

                return View["Admin", model];
            };

            Get["/Admin/GetUserList"] = parameters => Response.AsJson(_weaveAdmin.GetUserList());

            Get["/Admin/GetUserDetails/{userId}"] = parameters => {
                int userId;
                if (int.TryParse(parameters.userId, out userId)) {
                    return Response.AsJson(_weaveAdmin.GetUserDetails(userId));
                }

                return HttpStatusCode.BadRequest;
            };

            Post["/Admin/AddUser"] = parameters => {
                string user = Request.Form["login"];
                string pswd = Request.Form["password"];
                string pswd1 = Request.Form["password1"];

                if (pswd != pswd1) {
                    return "Passwords do not match.";
                }

                if (user.Contains("@")) {
                    return "Username cannot contain an \"@\" character.";
                }

                return _weaveAdmin.CreateUser(user, pswd, null);
            };

            Post["/Admin/DeleteUser/{userId}"] = parameters => {
                int userId;
                if (int.TryParse(parameters.userId, out userId)) {
                    return Response.AsJson(_weaveAdmin.DeleteUser(userId));
                }

                return HttpStatusCode.BadRequest;
            };

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