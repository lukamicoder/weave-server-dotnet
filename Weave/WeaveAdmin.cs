/* Copyright (C) 2010 Karoly Lukacs <lukamicoder@gmail.com>
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
using System.Configuration;
using System.Web.Script.Serialization;

namespace Weave {
    public class WeaveAdmin {
        JavaScriptSerializer _jss;
        WeaveStorageAdmin _db;

        public WeaveAdmin() {
            _jss = new JavaScriptSerializer();
            _db = new WeaveStorageAdmin();
        }

        public string GetCollectionListWithCounts(Int64 userId) {
            var dic = _db.GetCollectionListWithCounts(userId);
            return _jss.Serialize(dic);
        }

        public string GetUserList() {
            try {
                var list = _db.GetUserList();
                return _jss.Serialize(list);
            } catch (WeaveException ex) {
                return ex.Message;
            }
        }

        public string DeleteUser(Int64 userId) {
            string msg = "";
            if (!_db.DeleteUser(userId)) {
                msg = "There was an error on deleting user.";
            }

            return msg;
        }

        public string CreateUser(string userName, string password) {
            string msg = "";
            if (String.IsNullOrEmpty(userName) && String.IsNullOrEmpty(password)) {
                msg = "Username and password cannot be blank.";
            } else if (!WeaveValidation.IsValid(userName)) {
                msg = "Username can only consist of characters (A-Z or a-z), numbers (0-9), and these special characters: _ -.";
            } else if (!_db.IsUniqueUserName(userName)) {
                msg = "Username already exists.";
            } else if (!_db.CreateUser(userName, password)) {
                msg = String.Format("There was an error on adding {0}.", userName);
            }

            return msg;
        }

        public Int64 AuthenticateUser(string userName, string password) {
            try {
                if (_db.AuthenticateUser(userName, password)) {
                    return _db.UserId;
                }
            } catch (WeaveException) { }

            return 0;
        }

        public int Cleanup() {
            string daysBeforeDelete = ConfigurationManager.AppSettings["DaysBeforeDelete"];
            int days;
            if (!String.IsNullOrEmpty(daysBeforeDelete) && Int32.TryParse(daysBeforeDelete, out days) && days != 0) {
                return _db.Cleanup(days);
            }

            return -1;
        }
    }
}