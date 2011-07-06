/* Copyright (C) 2011 Karoly Lukacs <lukamicoder@gmail.com>
 *
 * Based on code created by Mozilla Labs.
 * 
 * This is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Library General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Library General Public License for more details.
 *
 * You should have received a copy of the GNU Library General Public
 * License along with this software; if not, write to the
 * Free Software Foundation, Inc., 59 Temple Place - Suite 330,
 * Boston, MA 02111-1307, USA.
 */

using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using WeaveCore;
using WeaveServer.Services;

namespace WeaveServer.Controllers {
    public class AdminController : Controller {
        WeaveAdmin _weaveAdmin = new WeaveAdmin();

        public AdminController() {
            _weaveAdmin.LogEvent += OnLogEvent;
        }

        public ActionResult Index(FormCollection form) {
            if (form.Count == 0) {
                return Request.IsAuthenticated ? View() : View("Login");
            }

            string user = form["login"];
            string pswd = form["password"];
            if (FormsAuthentication.Authenticate(user, pswd)) {
                FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(1, user, DateTime.Now, DateTime.Now.AddMinutes(30), false, "User");
                string cookieStr = FormsAuthentication.Encrypt(ticket);
                HttpCookie cookie = new HttpCookie(FormsAuthentication.FormsCookieName, cookieStr) { Path = FormsAuthentication.FormsCookiePath };
                Response.Cookies.Add(cookie);

                return RedirectToAction("Index", "Admin");
            }

            ViewBag.ErrorMessage = "Incorrect username and/or password.";
            ViewBag.ErrorDisplay = "block";

            return View("Login");
        }

        public ContentResult GetUserList() {
            return Content(_weaveAdmin.GetUserList());
        }

        [HttpPost]
        public ContentResult GetUserDetails(int userId) {
            return Content(_weaveAdmin.GetUserDetails(userId));
        }

        [HttpPost]
        public ContentResult AddUser(FormCollection form) {
            string user = form["login"];
            string pswd = form["password"];
            string pswd1 = form["password1"];

            if (pswd != pswd1) {
                return Content("Passwords do not match.");
            }

            return Content(_weaveAdmin.CreateUser(user, pswd));
        }

        [HttpPost]
        public ContentResult RemoveUser(int userId) {
            return Content(_weaveAdmin.DeleteUser(userId));
        }

        public ActionResult DeleteUser() {
            return View();
        }

        [HttpPost]
        public ActionResult DeleteUser(FormCollection form) {
            string user = form["login"];
            string pswd = form["password"];
            string result = "Incorrect username and/or password.";

            if (!String.IsNullOrEmpty(user) && !String.IsNullOrEmpty(pswd)) {
                Int32 id = _weaveAdmin.AuthenticateUser(user, pswd);
                if (id != 0) {
                    result = _weaveAdmin.DeleteUser(id);
                    if (result == "") {
                        result = String.Format("{0} has been deleted from the database.", user);
                        ViewBag.ResultStyle = "color: Black;";
                    }
                } 
            } 

            ViewBag.ResultMessage = result;

            return View("DeleteUser");
        }

        public ActionResult Logout() {
            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "Admin");
        }

        public ActionResult PageNotFound() {
            return View("PageNotFound");
        }

        private void OnLogEvent(object source, WeaveLogEventArgs args) {
            Logger.WriteMessage(args.Message, args.Type);
        }
    }
}