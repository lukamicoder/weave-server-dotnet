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

using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using NLog;
using WeaveCore;
using WeaveCore.Models;

namespace WeaveServer.Controllers {
    public class AccountController : Controller {
        WeaveAdmin _weaveAdmin = new WeaveAdmin();
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public AccountController() {
            _weaveAdmin.LogEvent += OnLogEvent;
        }

        private long GetUserId() {
            long userId = 0;
            if (Request != null && Request.IsAuthenticated) {
                var id = (FormsIdentity)HttpContext.User.Identity;
                FormsAuthenticationTicket ticket = id.Ticket;

                long.TryParse(ticket.UserData, out userId);
            }

            return userId;
        }

        public ActionResult Index(FormCollection form) {
            if (form.Count == 0) {
                if (Request.IsAuthenticated) {
                    return GetUserId() > 0 ? View() : Logout();
                }

                return View("Login");
            }

            string user = form["login"];
            string pswd = form["password"];
            long userId = _weaveAdmin.AuthenticateUser(user, pswd);
            if (userId > 0) {
                var ticket = new FormsAuthenticationTicket(1, user, DateTime.Now, DateTime.Now.AddMinutes(30), false, userId + "");
                string cookieStr = FormsAuthentication.Encrypt(ticket);
                var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, cookieStr) { Path = FormsAuthentication.FormsCookiePath };
                Response.Cookies.Add(cookie);

                return RedirectToAction("Index", "Account");
            }

            ViewBag.ErrorMessage = "Incorrect username and/or password.";
            ViewBag.ErrorDisplay = "block";

            return View("Login");
        }

        [HttpPost]
        public JsonResult GetUserSummary() {
            return Json(_weaveAdmin.GetUser(GetUserId()));
        }

        [HttpPost]
        public JsonResult GetUserDetails() {
            return Json(_weaveAdmin.GetUserDetails(GetUserId()));
        }

        [HttpPost]
        public ContentResult ChangePassword(string password) {
            string output = _weaveAdmin.ChangePassword(GetUserId(), password);

            return Content(output);
        }

        [HttpPost]
        public ContentResult ClearUserData() {
            string output = _weaveAdmin.ClearUserData(GetUserId());

            return Content(output);
        }

        [HttpPost]
        public ContentResult DeleteUser() {
            string output = _weaveAdmin.DeleteUser(GetUserId());

            return Content(output);
        }

        public ActionResult Logout() {
            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "Account");
        }

        public ActionResult PageNotFound() {
            return View("PageNotFound");
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