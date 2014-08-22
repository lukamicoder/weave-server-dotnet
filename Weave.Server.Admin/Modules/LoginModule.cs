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
using NLog;
using Weave.Core;
using Weave.Core.Models;

namespace Weave.Server.Admin.Modules {
    public class LoginModule : NancyModule {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        WeaveAdmin _weaveAdmin = new WeaveAdmin();

        public LoginModule() {
            _weaveAdmin.LogEvent += OnLogEvent;

            Get["/"] = parameters => Response.AsRedirect("Login");
            Get[@"/(.*)"] = parameters => Response.AsRedirect("Login");

            Get["/Login"] = parameters => {
                dynamic model = new ExpandoObject();
                model.ErrorMessage = "";
                model.ErrorDisplay = "none";

                return View["Login", model];
            };

            Post["/Login"] = parameters => {
                var mapper = new UserMapper(_weaveAdmin);
                var userGuid = mapper.ValidateUser((string)Request.Form.Login, (string)Request.Form.Password);
                if (userGuid != null) {
                    var user = (User) mapper.GetUserFromIdentifier(userGuid.Value, Context);
                    var list = (List<string>) user.Claims;

                    Request.Query.Remove("returnUrl");
                    return this.LoginAndRedirect(userGuid.Value, null, list.Contains("Admin") ? "Admin" : "Account");
                } 

                dynamic model = new ExpandoObject();
                model.ErrorMessage = "Incorrect username and/or password.";
                model.ErrorDisplay = "block";

                return View["Login", model];
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