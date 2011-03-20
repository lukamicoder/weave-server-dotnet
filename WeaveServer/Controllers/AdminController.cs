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
using Weave;

namespace WeaveServer.Controllers {
    public class AdminController : Controller {
        public ActionResult Index(FormCollection form) {
            if (form.Count == 0) {
                return Request.IsAuthenticated ? View() : View("Login");
            }

            string user = form["login"];
            string pswd = form["password"];
            if (FormsAuthentication.Authenticate(user, pswd)) {
                FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(1, user, DateTime.Now, DateTime.Now.AddMinutes(30), false, "User");
                string cookieStr = FormsAuthentication.Encrypt(ticket);
                HttpCookie cookie = new HttpCookie(FormsAuthentication.FormsCookieName, cookieStr);
                cookie.Path = FormsAuthentication.FormsCookiePath;
                Response.Cookies.Add(cookie);

                return RedirectToAction("Index", "Admin");
            }

            ViewData["errorMessage"] = "Incorrect username and/or password.";
            ViewData["errorDisplay"] = "block";

            return View("Login");
        }

        public ContentResult GetUserList() {
            string output;
            try {
                WeaveAdmin weaveAdmin = new WeaveAdmin();
                output = weaveAdmin.GetUserList();
            } catch (WeaveException x) {
                output =  "Error: " + x.Message;
            }

            return Content(output);
        }

        [HttpPost]
        public ContentResult GetUserDetails(int userId) {
            string output;
            try {
                WeaveAdmin weaveAdmin = new WeaveAdmin();
                output = weaveAdmin.GetUserDetails(userId);
            } catch (WeaveException x) {
                output = "Error: " + x.Message;
            }

            return Content(output);
        }

        [HttpPost]
        public ContentResult AddUser(FormCollection form) {
            string user = form["login"];
            string pswd = form["password"];
            WeaveAdmin weaveAdmin = new WeaveAdmin();

            return Content(weaveAdmin.CreateUser(user, pswd));
        }

        [HttpPost]
        public ContentResult RemoveUser(int userId) {
            WeaveAdmin weaveAdmin = new WeaveAdmin();

            return Content(weaveAdmin.DeleteUser(userId));
        }

        public ActionResult DeleteUser() {
            return View();
        }

        [HttpPost]
        public ActionResult DeleteUser(FormCollection form) {
            string user = form["login"];
            string pswd = form["password"];
            string result;

            if (!String.IsNullOrEmpty(user) && !String.IsNullOrEmpty(pswd)) {
                try {
                    WeaveAdmin weaveAdmin = new WeaveAdmin();

                    Int64 id = weaveAdmin.AuthenticateUser(user, pswd);
                    if (id != 0) {
                        result = weaveAdmin.DeleteUser(id);
                        if (result == "") {
                            result = String.Format("{0} has been deleted from the database.", user);
                            ViewData["resultStyle"] = "color: Black;";
                        }
                    } else {
                        result = "Incorrect username and/or password";
                    }
                } catch (WeaveException x) {
                    result = x.Message;
                }
            } else {
                result = "Incorrect username and/or password";
            }

            ViewData["resultMessage"] = result;

            return View("DeleteUser");
        }

        public ActionResult Cleanup() {
            if (Request.IsLocal) {
                WeaveAdmin weaveAdmin = new WeaveAdmin();
                
                return Content(weaveAdmin.Cleanup() + "");
            }

            return View("PageNotFound");
        }

        public ActionResult Logout() {
            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "Admin");
        }
    }
}