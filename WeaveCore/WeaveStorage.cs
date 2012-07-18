/* 
Weave Server.NET <http://code.google.com/p/weave-server-dotnet/>
Copyright (C) 2012 Karoly Lukacs

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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;

namespace WeaveCore {
    public class WeaveStorage {
        private readonly IWeaveStorage _ws;
        private readonly DatabaseType _databaseType;
        private readonly double _timeNow;
        private long _userId;

        public WeaveStorage() {
            _databaseType = ((WeaveConfigurationSection)ConfigurationManager.GetSection("WeaveDatabase")).DatabaseType;
            bool isConnString = false;

            switch (_databaseType) {
                case DatabaseType.SQLServer:
                    _ws = new WeaveStorageSQLServer();
                    isConnString = true;
                    break;
                case DatabaseType.SQLServerCe:
                    _ws = new WeaveStorageSQLServerCe();
                    break;
                case DatabaseType.SQLite:
                    _ws = new WeaveStorageSQLite();
                    break;
                case DatabaseType.MySQL:
                    _ws = new WeaveStorageMySQL();
                    isConnString = true;
                    break;
            }

            if (isConnString) {
                var connections = ConfigurationManager.ConnectionStrings;
                for (int x = 0; x < connections.Count; x++) {
                    if (connections[x].Name == "Weave") {
                        _ws.ConnString = connections[x].ConnectionString;
                        break;
                    }
                }
            }

            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);
            _timeNow = Math.Round(ts.TotalSeconds, 2);
            _ws.TimeNow = _timeNow;
        }

        #region Admin
        public long AuthenticateUser(string userName, string password) {
            _userId = _ws.AuthenticateUser(userName, password);
            return _userId;
        }

        public IEnumerable<UserResult> GetUserList() {
            return _ws.GetUserList();
        }

        public IEnumerable<UserResult> GetUserSummary(long userId) {
            return _ws.GetUserSummary(userId);
        }

        public IEnumerable<UserDetailResult> GetUserDetails(long userId) {
            return _ws.GetUserDetails(userId);
        }

        public void CreateUser(string userName, string password, string email) {
            _ws.CreateUser(userName, password, email);
        }

        public string GetUserName(long userId) {
            return _ws.GetUserName(userId);
        }

        public bool IsUserNameUnique(string userName) {
            return _ws.IsUserNameUnique(userName);
        }

        public void DeleteUser(long userId) {
            _ws.DeleteUser(userId);
        }

        public void ChangePassword(string password) {
            _ws.ChangePassword(_userId, password);
        }

        public void ChangePassword(long userId, string password) {
            _ws.ChangePassword(userId, password);
        }

        public void ClearUserData(long userId) {
            _ws.ClearUserData(userId);
        }
        #endregion

        #region Collection
        public double GetMaxTimestamp(string collection) {
            if (_userId == 0) {
                return 0;
            }
            return _ws.GetMaxTimestamp(collection, _userId);
        }

        public IEnumerable<string> GetCollectionList() {
            if (_userId == 0) {
                return null;
            }
            return _ws.GetCollectionList(_userId);
        }

        public Dictionary<string, double> GetCollectionListWithTimestamps() {
            if (_userId == 0) {
                return null;
            }
            return _ws.GetCollectionListWithTimestamps(_userId);
        }

        public Dictionary<string, long> GetCollectionListWithCounts() {
            if (_userId == 0) {
                return null;
            }
            return _ws.GetCollectionListWithCounts(_userId);
        }

        public double GetStorageTotal() {
            if (_userId == 0) {
                return 0;
            }
            return _ws.GetStorageTotal(_userId);
        }

        public Dictionary<string, int> GetCollectionStorageTotals() {
            if (_userId == 0) {
                return null;
            }
            return _ws.GetCollectionStorageTotals(_userId);
        }
        #endregion

        #region Wbo
        public void SaveWbo(WeaveBasicObject wbo) {
            if (_userId == 0) {
                return;
            }
            _ws.SaveWbo(wbo, _userId);
        }

        public void SaveWboList(Collection<WeaveBasicObject> wboList, WeaveResultList resultList) {
            if (_userId == 0) {
                return;
            }
            _ws.SaveWboList(wboList, resultList, _userId);
        }

        public void DeleteWbo(string id, string collection) {
            if (_userId == 0) {
                return;
            }
            _ws.DeleteWbo(id, collection, _userId);
        }

        public void DeleteWboList(string collection, string id, string newer, string older, string sort,
                                  string limit, string offset, string ids, string indexAbove, string indexBelow) {
            if (_userId == 0) {
                return;
            }
            _ws.DeleteWboList(collection, id, newer, older, sort, limit, offset, ids, indexAbove, indexBelow, _userId);
        }

        public WeaveBasicObject GetWbo(string collection, string id) {
            if (_userId == 0) {
                return null;
            }
            return _ws.GetWbo(collection, id, _userId);
        }

        public IList<WeaveBasicObject> GetWboList(string collection, string id, bool full, string newer, string older, string sort, string limit, string offset,
                                                       string ids, string indexAbove, string indexBelow) {
            if (_userId == 0) {
                return null;
            }
            return _ws.GetWboList(collection, id, full, newer, older, sort, limit, offset, ids, indexAbove, indexBelow, _userId);
        }
        #endregion
    }
}