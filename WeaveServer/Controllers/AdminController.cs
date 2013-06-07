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
    public class AdminController : Controller {
        WeaveAdmin _weaveAdmin = new WeaveAdmin();
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public AdminController() {
            _weaveAdmin.LogEvent += OnLogEvent;
        }

        public ActionResult Index(FormCollection form) {
            if (form.Count == 0) {
                if (Request.IsAuthenticated) {
                    FormsIdentity id = (FormsIdentity)HttpContext.User.Identity;
                    FormsAuthenticationTicket ticket = id.Ticket;

                    return ticket.UserData == "Admin" ? View() : Logout();
                }

                return View("Login");
            }

            string user = form["login"];
            string pswd = form["password"];
            if (FormsAuthentication.Authenticate(user, pswd)) {
                FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(1, user, DateTime.Now, DateTime.Now.AddMinutes(30), false, "Admin");
                string cookieStr = FormsAuthentication.Encrypt(ticket);
                HttpCookie cookie = new HttpCookie(FormsAuthentication.FormsCookieName, cookieStr) { Path = FormsAuthentication.FormsCookiePath };
                Response.Cookies.Add(cookie);

                return RedirectToAction("Index", "Admin");
            }

            ViewBag.ErrorMessage = "Incorrect username and/or password.";
            ViewBag.ErrorDisplay = "block";

            return View("Login");
        }

        [HttpPost]
        public JsonResult GetUserList() {
            return Json(_weaveAdmin.GetUserList());
        }

        public JsonResult GetUserDetails(int userId) {
            return Json(_weaveAdmin.GetUserDetails(userId));
        }

        [HttpPost]
        public ContentResult AddUser(FormCollection form) {
            string user = form["login"];
            string pswd = form["password"];
            string pswd1 = form["password1"];

            if (pswd != pswd1) {
                return Content("Passwords do not match.");
            }

            if (user.Contains("@")) {
                return Content("Username cannot contain an \"@\" character.");
            }

            return Content(_weaveAdmin.CreateUser(user, pswd, null));
        }

        [HttpPost]
        public ContentResult DeleteUser(int userId) {
            return Content(_weaveAdmin.DeleteUser(userId));
        }

        public ActionResult Logout() {
            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "Admin");
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