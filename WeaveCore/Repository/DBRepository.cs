using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Dapper;
using WeaveCore.Models;

namespace WeaveCore.Repository {
    public class DBRepository : BaseRepository {
        double TimeNow { get; set; }
        long UserID { get; set; }

        public DBRepository() {
            InitializeDatabase();
        }

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

            UserID = id;

            return id;
        }

        public IEnumerable<User> GetUserList() {
            List<User> list;

            const string sql = @"SELECT Users.UserId, Users.UserName, Users.Email, SUM(Wbos.PayloadSize) AS Total, MAX(Wbos.Modified) AS DateMax, MIN(Wbos.Modified) AS DateMin
						       FROM Users
						       LEFT JOIN Wbos ON Users.UserId = Wbos.UserId
						       GROUP BY Users.UserId, Users.UserName, Users.Email";

            using (var conn = GetConnection()) {
                list = conn.Query<dynamic>(sql).Select(u => new User {
                    UserId = u.UserId,
                    UserName = String.IsNullOrEmpty(u.Email) ? u.UserName : u.Email,
                    Payload = WeaveHelper.FormatPayloadSize(u.Total),
                    DateMin = u.DateMin == null ? 0 : u.DateMin * 1000,
                    DateMax = u.DateMax == null ? 0 : u.DateMax * 1000
                }).ToList();
            }

            return list;
        }

        public IEnumerable<User> GetUserSummary(long userId) {
            List<User> list;

            const string sql = @"SELECT Users.UserName, Users.Email, SUM(Wbos.PayloadSize) AS Total, MAX(Wbos.Modified) AS DateMax, MIN(Wbos.Modified) AS DateMin
						       FROM Users
						       LEFT JOIN Wbos ON Users.UserId = Wbos.UserId
                               Where Users.UserId = @userId
						       GROUP BY Users.UserName, Users.Email";

            using (var conn = GetConnection()) {
                list = conn.Query<dynamic>(sql, new { userId }).Select(u => new User {
                    UserId = userId,
                    UserName = String.IsNullOrEmpty(u.Email) ? u.UserName : u.Email,
                    Payload = WeaveHelper.FormatPayloadSize(u.Total),
                    DateMin = u.DateMin == null ? 0 : u.DateMin * 1000,
                    DateMax = u.DateMax == null ? 0 : u.DateMax * 1000
                }).ToList();
            }

            return list;
        }

        public IEnumerable<UserData> GetUserDetails(long userId) {
            List<UserData> list;

            const string sql = @"SELECT Collection, COUNT(*) AS Count, SUM(Wbos.PayloadSize) AS Total
    					       FROM Wbos
						       WHERE UserId = @userid
						       GROUP BY Collection";

            using (var conn = GetConnection()) {
                list = conn.Query<dynamic>(sql, new { userid = userId })
                    .Where(x => x.Collection != 2 && x.Collection != 5 && x.Collection != 6)
                    .Select(item => new UserData {
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
                conn.Execute(sql, new { userName, email, md5 = WeaveHelper.ConvertToHash(password) });
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

        public void DeleteUser(long userId) {
            const string sql = @"DELETE FROM Wbos WHERE UserId =  @userid;
						         DELETE FROM Users WHERE Users.UserId = @userid";

            using (var conn = GetConnection()) {
                var transaction = conn.BeginTransaction();

                conn.Execute(sql, new { userid = userId }, transaction);

                transaction.Commit();
            }
        }

        public void ChangePassword(string password, long userId = 0) {
            if (String.IsNullOrEmpty(password)) {
                return;
            }

            if (userId == 0) {
                userId = UserID;
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
        public double GetMaxTimestamp(string collection) {
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
                result = conn.Query<double>(sql, new { userid = UserID, ttl = TimeNow, collection = WeaveCollectionDictionary.GetKey(collection) })
                    .SingleOrDefault();
            }

            return Math.Round(result, 2);
        }

        public Dictionary<string, double> GetCollectionListWithTimestamps() {
            Dictionary<string, double> dic;

            const string sql = @"SELECT Collection, MAX(Modified) AS Timestamp 
    					       FROM Wbos
						       WHERE UserId = @userid
						       GROUP BY Collection";

            using (var conn = GetConnection()) {
                dic = conn.Query<dynamic>(sql, new { userid = UserID })
                    .ToDictionary(w => (string)WeaveCollectionDictionary.GetValue(w.Collection), w => (double)w.Timestamp);
            }

            return dic;
        }

        public Dictionary<string, long> GetCollectionListWithCounts() {
            Dictionary<string, long> dic;

            const string sql = @"SELECT Collection, COUNT(*) AS Count 
						       FROM Wbos
						       WHERE UserId = @userid
                               AND Ttl > @ttl
						       GROUP BY Collection";

            using (var conn = GetConnection()) {
                dic = conn.Query<dynamic>(sql, new { userid = UserID, ttl = TimeNow })
                    .ToDictionary(w => (string)WeaveCollectionDictionary.GetValue(w.Collection), w => (long)w.Count);
            }

            return dic;
        }

        public double GetStorageTotal() {
            long result;

            const string sql = @"SELECT SUM(Wbos.PayloadSize) 
						       FROM Wbos
						       INNER JOIN Users on Wbos.UserId = Users.UserId
						       WHERE Users.UserId = @userid
                               AND Ttl > @ttl";

            using (var conn = GetConnection()) {
                result = conn.Query<long>(sql, new { userid = UserID, ttl = TimeNow }).SingleOrDefault();
            }

            return Convert.ToDouble(result / 1024);
        }

        public Dictionary<string, int> GetCollectionStorageTotals() {
            Dictionary<string, int> dic;

            const string sql = @"SELECT Collection, SUM(PayloadSize) AS Total
						       FROM Wbos
						       WHERE UserId = @userid
                               AND Ttl > @ttl
						       GROUP BY Collection";

            using (var conn = GetConnection()) {
                dic = conn.Query<dynamic>(sql, new { userid = UserID, ttl = TimeNow })
                    .ToDictionary(w => (string)WeaveCollectionDictionary.GetValue(w.Collection), w => (int)w.Total / 1024);
            }

            return dic;
        }
        #endregion

        #region Wbo
        public void SaveWbo(WeaveBasicObject wbo) {
            string sql;

            if (DatabaseType == DatabaseType.SQLServer) {
                sql = @"UPDATE Wbos
                    SET SortIndex = @sortindex, 
                        Modified = @modified, 
                        Payload = @payload, 
                        PayloadSize = @payloadsize, 
                        Ttl = @ttl
                    WHERE UserID = @userid
                        AND Id = @id
                        AND Collection = @collection
                    IF (@@ROWCOUNT = 0)
                    BEGIN
	                    INSERT INTO Wbos (UserId, Id, Collection, SortIndex, Modified, Payload, PayloadSize, Ttl) 
				        VALUES (@userid, @id, @collection, @sortindex, @modified, @payload, @payloadsize, @ttl)
                    END";
            } else {
                sql = @"REPLACE INTO Wbos (UserId, Id, Collection, SortIndex, Modified, Payload, PayloadSize, Ttl) 
				    VALUES (@userid, @id, @collection, @sortindex, @modified, @payload, @payloadsize, @ttl)";
            }

            wbo.UserId = UserID;
            using (var conn = GetConnection()) {
                conn.Execute(sql, wbo);
            }
        }

        public void SaveWboList(Collection<WeaveBasicObject> wboList, WeaveResultList resultList) {
            if (wboList == null || wboList.Count <= 0) {
                return;
            }

            string sql;

            if (DatabaseType == DatabaseType.SQLServer) {
                sql = @"UPDATE Wbos
                    SET SortIndex = @sortindex, 
                        Modified = @modified, 
                        Payload = @payload, 
                        PayloadSize = @payloadsize, 
                        Ttl = @ttl
                    WHERE UserID = @userid
                        AND Id = @id
                        AND Collection = @collection
                    IF (@@ROWCOUNT = 0)
                    BEGIN
	                    INSERT INTO Wbos (UserId, Id, Collection, SortIndex, Modified, Payload, PayloadSize, Ttl) 
				        VALUES (@userid, @id, @collection, @sortindex, @modified, @payload, @payloadsize, @ttl)
                    END";
            } else {
                sql = @"REPLACE INTO Wbos (UserId, Id, Collection, SortIndex, Modified, Payload, PayloadSize, Ttl) 
				    VALUES (@userid, @id, @collection, @sortindex, @modified, @payload, @payloadsize, @ttl)";
            }

            using (var conn = GetConnection()) {
                var transaction = conn.BeginTransaction();

                foreach (var wbo in wboList) {
                    wbo.UserId = UserID;
                    try {
                        conn.Execute(sql, wbo, transaction);
                        resultList.SuccessIds.Add(wbo.Id);
                    } catch (Exception e) {
                        if (wbo.Id != null) {
                            resultList.FailedIds[wbo.Id] = new Collection<string> { e.Message };
                        }
                    }
                }

                transaction.Commit();
            }
        }

        public void DeleteWbo(string id, string collection) {
            const string sql = @"DELETE FROM Wbos
						       WHERE UserId = @userid
						       AND Collection = @collection 
						       AND Id = @id";

            using (var conn = GetConnection()) {
                conn.Execute(sql, new { userid = UserID, id = id, collection = WeaveCollectionDictionary.GetKey(collection) });
            }
        }

        public void DeleteWboList(string collection, string id, string newer, string older, string sort,
                                  string limit, string offset, string ids, string indexAbove, string indexBelow) {
            StringBuilder sb = new StringBuilder();

            using (var conn = GetConnection()) {
                sb.Append(@"DELETE FROM Wbos 
						    WHERE UserId = @userid
							AND Collection = @collection");

                var param = new DynamicParameters();
                param.Add("UserId", UserID);
                param.Add("Collection", WeaveCollectionDictionary.GetKey(collection));

                if (limit != null || offset != null || sort != null) {
                    IList<WeaveBasicObject> wboList = GetWboList(collection, id, false, newer, older, sort, limit, offset, ids, indexAbove, indexBelow);
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

        public WeaveBasicObject GetWbo(string collection, string id) {
            WeaveBasicObject wbo;

            const string sql = @"SELECT * FROM Wbos 
						       WHERE UserId = @userid 
						       AND Collection = @collection 
						       AND Id = @id
                               AND Ttl > @ttl";

            using (var conn = GetConnection()) {
                wbo = conn.Query<WeaveBasicObject>(sql, new { userid = UserID, ttl = TimeNow, collection = WeaveCollectionDictionary.GetKey(collection), id = id })
                    .SingleOrDefault();
            }

            return wbo;
        }

        public IList<WeaveBasicObject> GetWboList(string collection, string id, bool full, string newer = null, string older = null, string sort = null, string limit = null, string offset = null,
                                                       string ids = null, string indexAbove = null, string indexBelow = null) {
            List<WeaveBasicObject> wboList;
            var sb = new StringBuilder();

            using (var conn = GetConnection()) {
                if (DatabaseType == DatabaseType.SQLServer) {
                    sb.Append("SELECT ");

                    if (limit != null) {
                        sb.Append(" TOP(").Append(limit).Append(") ");
                    }

                    sb.Append(full ? "*" : "Id").Append(" FROM Wbos WHERE UserId = @userid AND Collection = @collection AND Ttl > @ttl");
                } else {
                    sb.Append("SELECT ").Append(full ? "*" : "Id").Append(" FROM Wbos WHERE UserId = @userid AND Collection = @collection AND Ttl > @ttl");
                }

                var param = new DynamicParameters();
                param.Add("UserId", UserID);
                param.Add("Ttl", TimeNow);
                param.Add("Collection", WeaveCollectionDictionary.GetKey(collection));

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

                switch (sort) {
                    case "index":
                        sb.Append(" ORDER BY SortIndex DESC");
                        break;
                    case "newest":
                        sb.Append(" ORDER BY Modified DESC");
                        break;
                    case "oldest":
                        sb.Append(" ORDER BY Modified");
                        break;
                }

                if (DatabaseType != DatabaseType.SQLServer) {
                    if (limit != null) {
                        sb.Append(" LIMIT ").Append(limit);
                        if (offset != null) {
                            sb.Append(" OFFSET ").Append(offset);
                        }
                    }
                }

                wboList = conn.Query<WeaveBasicObject>(sb.ToString(), param).ToList();
            }

            return wboList;
        }
        #endregion
    }
}