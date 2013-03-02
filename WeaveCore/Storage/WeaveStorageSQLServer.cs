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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Dapper;
using WeaveCore.Models;

namespace WeaveCore.Storage {
    class WeaveStorageSQLServer : WeaveStorageBase {
        public override IDbConnection GetConnection() {
            var conn = new SqlConnection(ConnString);
            conn.Open();

            return conn;
        }

        public override void DeleteUser(long userId) {
            const string sql = @"DELETE FROM Wbos WHERE UserId =  @userid;
						         DELETE FROM Users WHERE Users.UserId = @userid";

            using (var conn = GetConnection()) {
                conn.Execute(sql, new { userid = userId }); ;
            }
        }

        public override void SaveWbo(WeaveBasicObject wbo, long userId) {
            const string sql = @"MERGE INTO Wbos d
                                USING (SELECT @userid AS UserId, 
			                                  @id AS Id, 
			                                  @collection AS Collection, 
			                                  @sortindex AS SortIndex, 
			                                  @modified AS Modified, 
			                                  @payload AS Payload, 
			                                  @payloadsize AS PayloadSize, 
			                                  @ttl AS Ttl) s
                                ON s.UserId = d.UserId AND s.Id = d.Id AND s.Collection = d.Collection  
                                WHEN MATCHED THEN 
                                   UPDATE SET d.SortIndex = s.SortIndex, 
                                              d.Modified = s.Modified, 
                                              d.Payload = s.Payload, 
                                              d.PayloadSize = s.PayloadSize, 
                                              d.Ttl = s.Ttl
                                WHEN NOT MATCHED THEN 
                                   INSERT (UserId, Id, Collection, SortIndex, Modified, Payload, PayloadSize, Ttl) 
				                   VALUES (s.UserId, s.Id, s.Collection, s.SortIndex, s.Modified, s.Payload, s.PayloadSize, s.Ttl);";

            wbo.UserId = userId;
            using (var conn = GetConnection()) {
                conn.Execute(sql, wbo);
            }
        }

        public override void SaveWboList(Collection<WeaveBasicObject> wboList, WeaveResultList resultList, long userId) {
            if (wboList == null || wboList.Count <= 0) {
                return;
            }

            const string sql = @"MERGE INTO Wbos d
                                USING (SELECT @userid AS UserId, 
			                                  @id AS Id, 
			                                  @collection AS Collection, 
			                                  @sortindex AS SortIndex, 
			                                  @modified AS Modified, 
			                                  @payload AS Payload, 
			                                  @payloadsize AS PayloadSize, 
			                                  @ttl AS Ttl) s
                                ON s.UserId = d.UserId AND s.Id = d.Id AND s.Collection = d.Collection  
                                WHEN MATCHED THEN 
                                   UPDATE SET d.SortIndex = s.SortIndex, 
                                              d.Modified = s.Modified, 
                                              d.Payload = s.Payload, 
                                              d.PayloadSize = s.PayloadSize, 
                                              d.Ttl = s.Ttl
                                WHEN NOT MATCHED THEN 
                                   INSERT (UserId, Id, Collection, SortIndex, Modified, Payload, PayloadSize, Ttl) 
				                   VALUES (s.UserId, s.Id, s.Collection, s.SortIndex, s.Modified, s.Payload, s.PayloadSize, s.Ttl);";

            using (var conn = GetConnection()) {
                var transaction = conn.BeginTransaction();

                foreach (WeaveBasicObject wbo in wboList) {
                    wbo.UserId = userId;
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