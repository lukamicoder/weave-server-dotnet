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
    public class AccountController : Controller {
        WeaveAdmin _weaveAdmin = new WeaveAdmin();

        public AccountController() {
            _weaveAdmin.LogEvent += OnLogEvent;
        }

        private Int32 GetUserId() {
            int userId = 0;
            if (Request != null && Request.IsAuthenticated) {
                FormsIdentity id = (FormsIdentity) HttpContext.User.Identity;
                FormsAuthenticationTicket ticket = id.Ticket;

                Int32.TryParse(ticket.UserData, out userId);
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
            int userId = _weaveAdmin.AuthenticateUser(user, pswd);
            if (userId > 0) {
                FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(1, user, DateTime.Now, DateTime.Now.AddMinutes(30), false, userId + "");
                string cookieStr = FormsAuthentication.Encrypt(ticket);
                HttpCookie cookie = new HttpCookie(FormsAuthentication.FormsCookieName, cookieStr) { Path = FormsAuthentication.FormsCookiePath };
                Response.Cookies.Add(cookie);

                return RedirectToAction("Index", "Account");
            }

            ViewBag.ErrorMessage = "Incorrect username and/or password.";
            ViewBag.ErrorDisplay = "block";

            return View("Login");
        }

        [HttpPost]
        public ContentResult GetUserSummary() {
            string output = _weaveAdmin.GetUserSummary(GetUserId());

            return Content(output);
        }

        [HttpPost]
        public ContentResult GetUserDetails() {
            string output = _weaveAdmin.GetUserDetails(GetUserId());

            return Content(output);
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

        private void OnLogEvent(object source, WeaveLogEventArgs args) {
            Logger.WriteMessage(args.Message, args.Type);
        }
    }
}