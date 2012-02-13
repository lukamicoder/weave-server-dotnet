﻿/* Copyright (C) 2011 Karoly Lukacs <lukamicoder@gmail.com>
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
using System.Web.Script.Serialization;

namespace WeaveCore {
    public class WeaveAdmin : WeaveLogEventBase {
        JavaScriptSerializer _jss;
        WeaveAdminStorage _db;

        public WeaveAdmin() {
            _jss = new JavaScriptSerializer();
            _db = new WeaveAdminStorage();
            _db.LogEvent += OnLogEvent;
        }

        public Int32 AuthenticateUser(string userName, string password) {
            return _db.AuthenticateUser(userName, password);
        }

        public string GetUserList() {
            try {
                var list = _db.GetUserList();
                return _jss.Serialize(list);
            } catch (WeaveException ex) {
                return String.Format("Error: {0}", ex.Message);
            }
        }

        public string GetUserSummary(Int32 userId) {
            try {
                var list = _db.GetUserSummary(userId);
                return _jss.Serialize(list);
            } catch (WeaveException ex) {
                return String.Format("Error: {0}", ex.Message);
            }
        }

        public string GetUserDetails(Int32 userId) {
            try {
                var list = _db.GetUserDetails(userId);
                return _jss.Serialize(list);
            } catch (WeaveException ex) {
                return String.Format("Error: {0}", ex.Message);
            }
        }

        public string CreateUser(string userName, string password, string email) {
            string msg = "";
            if (String.IsNullOrEmpty(userName) || String.IsNullOrEmpty(password)) {
                msg = "Username and password cannot be blank.";
            } else if (!userName.Contains("@") && !WeaveHelper.IsUserNameValid(userName)) {
                msg = "Username can only consist of characters (A-Z or a-z), numbers (0-9), and these special characters: _ -.";
            } else if (!IsUserNameUnique(userName)) {
                msg = "Username already exists.";
            } else {
                if (!_db.CreateUser(userName, password, email)) {
                    msg = String.Format("There was an error on adding {0}.", userName);
                }
            }

            return msg;
        }

        public bool IsUserNameUnique(string userName) {
            return _db.IsUserNameUnique(userName);
        }

        public string DeleteUser(Int32 userId) {
            string msg = "";
            if (!_db.DeleteUser(userId)) {
                msg = "There was an error on deleting user.";
            }

            return msg;
        }

        public string ChangePassword(Int32 userId, string password) {
            try {
                _db.ChangePassword(userId, password);
            } catch (WeaveException ex) {
                return String.Format("Error: {0}", ex.Message);
            }

            return "";
        }

        public string ClearUserData(Int32 userId) {
            try {
                _db.ClearUserData(userId);
            } catch (WeaveException ex) {
                return String.Format("Error: {0}", ex.Message);
            }

            return "";
        }
    }
}