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
using System.Data.SqlServerCe;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Dapper;

namespace WeaveCore {
    class WeaveStorageSQLServerCe : WeaveStorageBase {
        private static readonly object Lock = new object();

        public WeaveStorageSQLServerCe() {
            InitializeDatabase();
        }

        protected void InitializeDatabase() {
            string dir = AppDomain.CurrentDomain.GetData("DataDirectory") as string;

            if (string.IsNullOrEmpty(dir)) {
                dir = AppDomain.CurrentDomain.BaseDirectory;
            }

            string dbName = dir + Path.DirectorySeparatorChar + "weave.sdf";

            ConnString = "Data Source=" + dbName + ";";

            lock (Lock) {
                var dbFile = new FileInfo(dbName);
                if (dbFile.Exists) {
                    return;
                }

                var engine = new SqlCeEngine(ConnString);
                engine.CreateDatabase();
                engine.Dispose();

                CreateTables();
            }
        }

        private void CreateTables() {
            const string sql = @"
                CREATE TABLE Wbos (UserId bigint NOT NULL,
                                   Id nvarchar(64) NOT NULL,
                                   Collection smallint NOT NULL,
                                   Modified float NULL,
                                   SortIndex bigint NULL,
                                   Payload ntext NULL,
                                   PayloadSize bigint NULL,
                                   Ttl float NULL,
                                   PRIMARY KEY (UserId, Collection, Id))";
            const string sql1 = @"
                CREATE TABLE Users (UserId bigint IDENTITY NOT NULL PRIMARY KEY, 
                                   UserName nvarchar(32) NOT NULL, 
                                   Md5 nvarchar(128) NOT NULL, 
                                   Email nvarchar(64) NULL)";
            const string sql2 = @"
                CREATE INDEX modifiedindex ON Wbos (UserId, Collection, Modified)";

            using (var conn = GetConnection()) {
                try {
                    conn.Execute(sql);
                    conn.Execute(sql1);
                    conn.Execute(sql2);
                } catch (Exception ex) {
                    Debug.WriteLine(ex.ToString());
                }
            }
        }

        public override IDbConnection GetConnection() {
            var conn = new SqlCeConnection(ConnString);
            conn.Open();

            return conn;
        }

        public override void DeleteUser(long userId) {
            const string sql = "DELETE FROM Wbos WHERE UserId =  @userid";
			const string sql1 = "DELETE FROM Users WHERE Users.UserId = @userid";

            using (var conn = GetConnection()) {
                var transaction = conn.BeginTransaction();

                conn.Execute(sql, new { userid = userId });
                conn.Execute(sql1, new { userid = userId });

                transaction.Commit();
            }
        }

        public override void SaveWbo(WeaveBasicObject wbo, long userId) {
            const string sqlSelect = @"SELECT COUNT(*) FROM Wbos WHERE UserId = @userid AND Collection = @collection AND Id = @id";
            const string sqlInsert = @"INSERT INTO Wbos (UserId, Id, Collection, SortIndex, Modified, Payload, PayloadSize, Ttl) 
				                       VALUES (@userid, @id, @collection, @sortindex, @modified, @payload, @payloadsize, @ttl)";
            const string sqlUpdate = @"UPDATE Wbos 
                                       SET SortIndex = @sortindex, 
                                           Modified = @modified, 
                                           Payload = @payload, 
                                           PayloadSize = @payloadsize, 
                                           Ttl = @ttl
                                       WHERE UserId = @userid AND Collection = @collection AND Id = @id";

            wbo.UserId = userId;
            using (var conn = GetConnection()) {
                int count = conn.Query<int>(sqlSelect, new { userid = userId, collection = wbo.Collection, id = wbo.Id }).SingleOrDefault();
                if (count > 0) {
                    conn.Execute(sqlUpdate, wbo);
                } else {
                    conn.Execute(sqlInsert, wbo);
                }
            }
        }

        public override void SaveWboList(Collection<WeaveBasicObject> wboList, WeaveResultList resultList, long userId) {
            if (wboList == null || wboList.Count <= 0) {
                return;
            }

            const string sqlSelect = @"SELECT COUNT(*) FROM Wbos WHERE UserId = @userid AND Collection = @collection AND Id = @id";
            const string sqlInsert = @"INSERT INTO Wbos (UserId, Id, Collection, SortIndex, Modified, Payload, PayloadSize, Ttl) 
				                       VALUES (@userid, @id, @collection, @sortindex, @modified, @payload, @payloadsize, @ttl)";
            const string sqlUpdate = @"UPDATE Wbos 
                                       SET SortIndex = @sortindex, 
                                           Modified = @modified, 
                                           Payload = @payload, 
                                           PayloadSize = @payloadsize, 
                                           Ttl = @ttl
                                       WHERE UserId = @userid AND Collection = @collection AND Id = @id";

            using (var conn = GetConnection()) {
                var transaction = conn.BeginTransaction();

                foreach (WeaveBasicObject wbo in wboList) {
                    wbo.UserId = userId;
                    try {
                        int count = conn.Query<int>(sqlSelect, new { userid = userId, collection = wbo.Collection, id = wbo.Id }).SingleOrDefault();
                        if (count > 0) {
                            conn.Execute(sqlUpdate, wbo, transaction);
                        } else {
                            conn.Execute(sqlInsert, wbo, transaction);
                        }
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

        public override IList<WeaveBasicObject> GetWboList(string collection, string id, bool full, string newer, string older, string sort, string limit, string offset,
                                                       string ids, string indexAbove, string indexBelow, long userId) {
            List<WeaveBasicObject> wboList;
            StringBuilder sb = new StringBuilder();

            using (var conn = GetConnection()) {
                sb.Append("SELECT ");

                if (limit != null) {
                    sb.Append(" TOP(").Append(limit).Append(") ");
                }

                sb.Append(full ? "*" : "Id").Append(" FROM Wbos WHERE UserId = @userid AND Collection = @collection AND Ttl > @ttl");

                var param = new DynamicParameters();
                param.Add("UserId", userId);
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

                wboList = conn.Query<WeaveBasicObject>(sb.ToString(), param).ToList();
            }

            return wboList;
        }
    }
}