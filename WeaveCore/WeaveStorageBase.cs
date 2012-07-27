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
using System.Data;
using System.Linq;
using System.Text;
using Dapper;

namespace WeaveCore {
    abstract class WeaveStorageBase : IWeaveStorage {
        public string ConnString { get; set; }
        public double TimeNow { get; set; }

        public abstract IDbConnection GetConnection();

        #region Admin
        public long AuthenticateUser(string userName, string password) {
            long id = 0;

            string sql;
            if (userName.Contains("@")) {
                sql = @"SELECT Users.UserId 
						FROM Users 
						WHERE Users.Email = @username AND Md5 = @md5";
            } else {
                sql = @"SELECT Users.UserId 
						FROM Users 
						WHERE Users.UserName = @username AND Md5 = @md5";
            }

            using (var conn = GetConnection()) {
                var result = conn.Query<long?>(sql, new { username = userName, md5 = WeaveHelper.ConvertToHash(password) }).SingleOrDefault();
                if (result != null) {
                    id = result.Value;
                }
            }

            return id;
        }

        public IEnumerable<UserResult> GetUserList() {
            List<UserResult> list;

            const string sql = @"SELECT Users.UserId, Users.UserName, Users.Email, SUM(Wbos.PayloadSize) AS Total, MAX(Wbos.Modified) AS DateMax, MIN(Wbos.Modified) AS DateMin
						       FROM Users
						       LEFT JOIN Wbos ON Users.UserId = Wbos.UserId
						       GROUP BY Users.UserId, Users.UserName, Users.Email";

            using (var conn = GetConnection()) {
                list = conn.Query<dynamic>(sql).Select(u => new UserResult {
                    UserId = u.UserId,
                    UserName = String.IsNullOrEmpty(u.Email) ? u.UserName : u.Email,
                    Payload = WeaveHelper.FormatPayloadSize(u.Total),
                    DateMin = u.DateMin == null ? 0 : u.DateMin * 1000,
                    DateMax = u.DateMax == null ? 0 : u.DateMax * 1000
                }).ToList();
            }

            return list;
        }

        public IEnumerable<UserResult> GetUserSummary(long userId) {
            List<UserResult> list;

            const string sql = @"SELECT Users.UserName, Users.Email, SUM(Wbos.PayloadSize) AS Total, MAX(Wbos.Modified) AS DateMax, MIN(Wbos.Modified) AS DateMin
						       FROM Users
						       LEFT JOIN Wbos ON Users.UserId = Wbos.UserId
                               Where Users.UserId = @userId
						       GROUP BY Users.UserName, Users.Email";

            using (var conn = GetConnection()) {
                list = conn.Query<dynamic>(sql, new { userId }).Select(u => new UserResult {
                    UserId = userId,
                    UserName = String.IsNullOrEmpty(u.Email) ? u.UserName : u.Email,
                    Payload = WeaveHelper.FormatPayloadSize(u.Total),
                    DateMin = u.DateMin == null ? 0 : u.DateMin * 1000,
                    DateMax = u.DateMax == null ? 0 : u.DateMax * 1000
                }).ToList();
            }

            return list;
        }

        public IEnumerable<UserDetailResult> GetUserDetails(long userId) {
            List<UserDetailResult> list;

            const string sql = @"SELECT Collection, COUNT(*) AS Count, SUM(Wbos.PayloadSize) AS Total
    					       FROM Wbos
						       WHERE UserId = @userid
						       GROUP BY Collection";

            using (var conn = GetConnection()) {
                list = conn.Query<dynamic>(sql, new { userid = userId })
                    .Where(x => x.Collection != 2 && x.Collection != 5 && x.Collection != 6)
                    .Select(item => new UserDetailResult {
                        Collection = WeaveCollectionDictionary.GetValue(item.Collection),
                        Count = item.Count,
                        Payload = WeaveHelper.FormatPayloadSize(item.Total)
                    }).ToList();
            }

            return list;
        }

        public void CreateUser(string userName, string password, string email) {
            if (String.IsNullOrEmpty(userName)) {
                return;
            }

            const string sql = "INSERT INTO Users (UserName, Email, Md5) VALUES (@userName, @email, @md5)";

            using (var conn = GetConnection()) {
                conn.Execute(sql, new {userName, email, md5 = WeaveHelper.ConvertToHash(password)});
            }
        }

        public string GetUserName(long userId) {
            string name;

            const string sql = "SELECT UserName FROM Users WHERE UserId = @userid";

            using (var conn = GetConnection()) {
                name = conn.Query<string>(sql, new { UserId = userId }).SingleOrDefault();
            }

            return name;
        }

        public bool IsUserNameUnique(string userName) {
            bool result = false;

            const string sql = "SELECT UserName FROM Users WHERE UserName = @username";

            using (var conn = GetConnection()) {
                var name = conn.Query<string>(sql, new { username = userName }).SingleOrDefault();

                if (String.IsNullOrEmpty(name)) {
                    result = true;
                }
            }

            return result;
        }

        public abstract void DeleteUser(long userId);

        public void ChangePassword(long userId, string password) {
            if (String.IsNullOrEmpty(password)) {
                return;
            }

            const string sql = @"UPDATE Users SET Md5 = @md5 WHERE UserId = @userid";

            using (var conn = GetConnection()) {
                conn.Execute(sql, new { md5 = WeaveHelper.ConvertToHash(password), userid = userId });
            }
        }

        public void ClearUserData(long userId) {
            const string sql = @"DELETE FROM Wbos WHERE  UserId =  @userid";

            using (var conn = GetConnection()) {
                conn.Execute(sql, new { userid = userId });
            }
        }
        #endregion

        #region Collection
        public double GetMaxTimestamp(string collection, long userId) {
            double result;

            if (String.IsNullOrEmpty(collection)) {
                return 0;
            }

            const string sql = @"SELECT MAX(Modified) 
						       FROM Wbos
						       WHERE UserId = @userid
                               AND Ttl > @ttl
						       AND Collection = @collection";

            using (var conn = GetConnection()) {
                result = conn.Query<double>(sql, new { userid = userId, ttl = TimeNow, collection = WeaveCollectionDictionary.GetKey(collection) })
                    .SingleOrDefault();
            }

            return Math.Round(result, 2);
        }

        public IEnumerable<string> GetCollectionList(long userId) {
            List<string> list;

            const string sql = "SELECT DISTINCT(Collection) FROM Wbos WHERE UserId = @userid";

            using (var conn = GetConnection()) {
                list = conn.Query<short>(sql, new { userid = userId }).Select(WeaveCollectionDictionary.GetValue).ToList();
            }

            return list;
        }

        public Dictionary<string, double> GetCollectionListWithTimestamps(long userId) {
            Dictionary<string, double> dic;

            const string sql = @"SELECT Collection, MAX(Modified) AS Timestamp 
    					       FROM Wbos
						       WHERE UserId = @userid
						       GROUP BY Collection";

            using (var conn = GetConnection()) {
                dic = conn.Query<dynamic>(sql, new { userid = userId })
                    .ToDictionary(w => (string)WeaveCollectionDictionary.GetValue(w.Collection), w => (double)w.Timestamp);
            }

            return dic;
        }

        public Dictionary<string, long> GetCollectionListWithCounts(long userId) {
            Dictionary<string, long> dic;

            const string sql = @"SELECT Collection, COUNT(*) AS Count 
						       FROM Wbos
						       WHERE UserId = @userid
                               AND Ttl > @ttl
						       GROUP BY Collection";

            using (var conn = GetConnection()) {
                dic = conn.Query<dynamic>(sql, new { userid = userId, ttl = TimeNow })
                    .ToDictionary(w => (string)WeaveCollectionDictionary.GetValue(w.Collection), w => (long)w.Count);
            }

            return dic;
        }

        public double GetStorageTotal(long userId) {
            long result;

            const string sql = @"SELECT SUM(Wbos.PayloadSize) 
						       FROM Wbos
						       INNER JOIN Users on Wbos.UserId = Users.UserId
						       WHERE Users.UserId = @userid
                               AND Ttl > @ttl";

            using (var conn = GetConnection()) {
                result = conn.Query<long>(sql, new { userid = userId, ttl = TimeNow }).SingleOrDefault();
            }

            return Convert.ToDouble(result / 1024);
        }

        public Dictionary<string, int> GetCollectionStorageTotals(long userId) {
            Dictionary<string, int> dic;

            const string sql = @"SELECT Collection, SUM(PayloadSize) AS Total
						       FROM Wbos
						       WHERE UserId = @userid
                               AND Ttl > @ttl
						       GROUP BY Collection";

            using (var conn = GetConnection()) {
                dic = conn.Query<dynamic>(sql, new { userid = userId, ttl = TimeNow })
                    .ToDictionary(w => (string)WeaveCollectionDictionary.GetValue(w.Collection), w => (int)w.Total / 1024);
            }

            return dic;
        }
        #endregion

        #region Wbo
        public abstract void SaveWbo(WeaveBasicObject wbo, long userId);

        public abstract void SaveWboList(Collection<WeaveBasicObject> wboList, WeaveResultList resultList, long userId);

        public void DeleteWbo(string id, string collection, long userId) {
            const string sql = @"DELETE FROM Wbos
						       WHERE UserId = @userid
						       AND Collection = @collection 
						       AND Id = @id";

            using (var conn = GetConnection()) {
                conn.Execute(sql, new { userid = userId, id = id, collection = WeaveCollectionDictionary.GetKey(collection) });
            }
        }

        public void DeleteWboList(string collection, string id, string newer, string older, string sort,
                                  string limit, string offset, string ids, string indexAbove, string indexBelow, long userId) {
            StringBuilder sb = new StringBuilder();

            using (var conn = GetConnection()) {
                sb.Append(@"DELETE FROM Wbos 
						    WHERE UserId = @userid
							AND Collection = @collection");

                var param = new DynamicParameters();
                param.Add("UserId", userId);
                param.Add("Collection", WeaveCollectionDictionary.GetKey(collection));

                if (limit != null || offset != null || sort != null) {
                    IList<WeaveBasicObject> wboList = GetWboList(collection, id, false, newer, older, sort, limit, offset, ids, indexAbove, indexBelow, userId);
                    if (wboList.Count > 0) {
                        sb.Append(" AND Id IN (@ids)");
                        var idArray = new string[wboList.Count];
                        for (int x = 0; x < wboList.Count; x++) {
                            idArray[x] = wboList[x].Id;
                        }
                        param.Add("Ids", idArray);
                    }
                } else {
                    if (id != null) {
                        sb.Append(" AND Id = @id");
                        param.Add("Id", id);
                    }

                    if (ids != null) {
                        sb.Append(" AND Id IN (@ids)");
                        param.Add("Ids", ids.Split(new[] { ',' }));
                    }

                    if (indexAbove != null) {
                        sb.Append("AND SortIndex > @indexAbove");
                        param.Add("IndexAbove", Convert.ToInt64(indexAbove));
                    }

                    if (indexBelow != null) {
                        sb.Append(" AND SortIndex < @indexBelow");
                        param.Add("IndexBelow", Convert.ToInt64(indexBelow));
                    }

                    if (newer != null) {
                        sb.Append(" AND Modified > @newer");
                        param.Add("Newer", Convert.ToDouble(newer));
                    }

                    if (older != null) {
                        sb.Append(" AND Modified < @older");
                        param.Add("Older", Convert.ToDouble(older));
                    }
                }

                conn.Execute(sb.ToString(), param);
            }
        }

        public WeaveBasicObject GetWbo(string collection, string id, long userId) {
            WeaveBasicObject wbo;

            const string sql = @"SELECT * FROM Wbos 
						       WHERE UserId = @userid 
						       AND Collection = @collection 
						       AND Id = @id
                               AND Ttl > @ttl";

            using (var conn = GetConnection()) {
                wbo = conn.Query<WeaveBasicObject>(sql, new { userid = userId, ttl = TimeNow, collection = WeaveCollectionDictionary.GetKey(collection), id = id })
                    .SingleOrDefault();
            }

            return wbo;
        }

        public abstract IList<WeaveBasicObject> GetWboList(string collection, string id, bool full, string newer, string older, string sort, string limit, string offset,
                                                       string ids, string indexAbove, string indexBelow, long userId);
        #endregion
    }
}