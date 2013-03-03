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
using Newtonsoft.Json;
using WeaveCore.Repository;

namespace WeaveCore {
    public class WeaveAdmin : WeaveLogEventBase {
        readonly DBRepository _db;

        public WeaveAdmin() {
            _db = new DBRepository();
        }

        public long AuthenticateUser(string userName, string password) {
            return _db.AuthenticateUser(userName, password);
        }

        public string GetUserList() {
            try {
                var list = _db.GetUserList();
                return JsonConvert.SerializeObject(list);
            } catch (Exception x) {
                RaiseLogEvent(this, x.ToString(), LogType.Error);
                return String.Format("Error: {0}", x.Message);
            }
        }

        public string GetUserSummary(long userId) {
            try {
                var list = _db.GetUserSummary(userId);
                return JsonConvert.SerializeObject(list);
            } catch (Exception x) {
                RaiseLogEvent(this, x.ToString(), LogType.Error);
                return String.Format("Error: {0}", x.Message);
            }
        }

        public string GetUserDetails(long userId) {
            try {
                var list = _db.GetUserDetails(userId);
                return JsonConvert.SerializeObject(list);
            } catch (Exception x) {
                RaiseLogEvent(this, x.ToString(), LogType.Error);
                return String.Format("Error: {0}", x.Message);
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
                try {
                    _db.CreateUser(userName, password, email);
                    RaiseLogEvent(this, String.Format("{0} user account has been created.", String.IsNullOrEmpty(email) ? userName : email), LogType.Information);
                } catch (Exception x) {
                    RaiseLogEvent(this, x.ToString(), LogType.Error);
                    msg = String.Format("There was an error on adding {0}.", userName);
                }
            }

            return msg;
        }

        public bool IsUserNameUnique(string userName) {
            return _db.IsUserNameUnique(userName);
        }

        public string DeleteUser(long userId) {
            string msg = "";
            string userName = "";

            try {
                userName = _db.GetUserName(userId);
                _db.DeleteUser(userId);
            } catch (Exception x) {
                RaiseLogEvent(this, x.ToString(), LogType.Error);
                msg = "There was an error on deleting user.";
            }

            if (String.IsNullOrEmpty(msg)) {
                RaiseLogEvent(this, String.Format("{0} user account has been deleted.", userName), LogType.Information);
            }

            return msg;
        }

        public string ChangePassword(long userId, string password) {
            try {
                _db.ChangePassword(password, userId);
            } catch (Exception x) {
                RaiseLogEvent(this, x.ToString(), LogType.Error);
                return String.Format("Error: {0}", x.Message);
            }

            return "";
        }

        public string ClearUserData(long userId) {
            try {
                _db.ClearUserData(userId);
            } catch (Exception x) {
                RaiseLogEvent(this, x.ToString(), LogType.Error);
                return String.Format("Error: {0}", x.Message);
            }

            return "";
        }
    }
}