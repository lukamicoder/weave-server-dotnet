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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SQLite;
using System.Text;

namespace Weave {
	class WeaveStorageUser : WeaveStorage {
		public string UserName { get; set; }

		public WeaveStorageUser(string userName) {
			UserName = userName;
			SetupDatabase();
		}

		public double GetMaxTimestamp(string collection) {
			double result = 0;

			if (String.IsNullOrEmpty(collection)){
				return 0;
			}

			using (SQLiteConnection conn = new SQLiteConnection(ConnString))
			using (SQLiteCommand cmd = new SQLiteCommand(@"SELECT MAX(Modified) 
														  FROM Wbos
														  WHERE UserId = @userid
														  AND Collection = @collection", conn)) {
				try {
					cmd.Parameters.Add("@userid", DbType.Int32).Value = UserId;
					cmd.Parameters.Add("@collection", DbType.Int16).Value = WeaveCollectionDictionary.GetKey(collection); 

					conn.Open();
					Object obj = cmd.ExecuteScalar();
					if (obj != null) {
						result = (double)obj;
					}
				} catch (SQLiteException x) {
					WeaveLogger.WriteMessage(x.Message, LogType.Error);
					throw new WeaveException("Database unavailable", 503);
				}
			}
			
			return Math.Round(result, 2);
		}

		public IList<string> GetCollectionList() {
			IList<string> list = new List<string>();

			using (SQLiteConnection conn = new SQLiteConnection(ConnString))
			using (SQLiteCommand cmd = new SQLiteCommand("SELECT DISTINCT(Collection) FROM Wbos WHERE UserId = @userid", conn)) {
				try {
					cmd.Parameters.Add("@userid", DbType.Int32).Value = UserId;

					conn.Open();
					using (SQLiteDataReader reader = cmd.ExecuteReader()) {
						if (reader.HasRows) {
							while (reader.Read()) {
								if (reader["Collection"] != DBNull.Value) {
									string coll = WeaveCollectionDictionary.GetValue((short)reader["Collection"]);
									list.Add(coll);
								}
							}
						}
					}
				} catch (SQLiteException x) {
					WeaveLogger.WriteMessage(x.Message, LogType.Error);
					throw new WeaveException("Database unavailable", 503);
				}
			}

			return list;
		}

		public Dictionary<string, double> GetCollectionListWithTimestamps() {
			Dictionary<string, double> dic = new Dictionary<string, double>();

			using (SQLiteConnection conn = new SQLiteConnection(ConnString))
			using (SQLiteCommand cmd = new SQLiteCommand(@"SELECT Collection, MAX(Modified) AS Timestamp 
														  FROM Wbos
														  WHERE UserId = @userid
														  GROUP BY Collection", conn)) {
				try {
					cmd.Parameters.Add("@userid", DbType.Int32).Value = UserId;

					conn.Open();
					using (SQLiteDataReader reader = cmd.ExecuteReader()) {
						if (reader.HasRows) {
							while (reader.Read()) {
								if (reader["Collection"] != DBNull.Value && reader["Timestamp"] != null) {
									string coll = WeaveCollectionDictionary.GetValue((short)reader["Collection"]);
									dic.Add(coll, (double)reader["Timestamp"]);
								}
							}
						}
					}
				} catch (SQLiteException x) {
					WeaveLogger.WriteMessage(x.Message, LogType.Error);
					throw new WeaveException("Database unavailable", 503);
				}
			}

			return dic;
		}

		public Dictionary<string, long> GetCollectionListWithCounts() {
			return GetCollectionListWithCounts(UserName);
		}

		public void StoreOrUpdateWbo(WeaveBasicObject wbo) {
			using (SQLiteConnection conn = new SQLiteConnection(ConnString))
			using (SQLiteCommand cmd = new SQLiteCommand(conn)) {
				conn.Open();

				AddParameters(cmd);

				try {
					//if there's no payload (as opposed to blank), then update the metadata
					if (wbo.Payload != null) {
						StoreWbo(cmd, wbo);
					} else {
						UpdateWbo(cmd, wbo);
					}
				} catch (SQLiteException e) {
					WeaveLogger.WriteMessage(e.Message, LogType.Error);
					throw new WeaveException("Database unavailable", 503);
				}
			}
		}

		public void StoreOrUpdateWboList(Collection<WeaveBasicObject> wboList, WeaveResultList resultList) {
			if (wboList != null && wboList.Count > 0) {
				using (SQLiteConnection conn = new SQLiteConnection(ConnString))
				using (SQLiteCommand cmd = new SQLiteCommand(conn)) {
					conn.Open();

					AddParameters(cmd);

					using (SQLiteTransaction trans = conn.BeginTransaction()) {
						foreach (WeaveBasicObject wbo in wboList) {
							try {
								if (wbo.Payload != null) {
									StoreWbo(cmd, wbo);
								} else {
									UpdateWbo(cmd, wbo);
								}
							} catch (SQLiteException e) {
								if (wbo.Id != null) {
									resultList.FailedIds[wbo.Id] = new Collection<string> { e.Message };
								}
								continue;
							}

							resultList.SuccessIds.Add(wbo.Id);
						}

						try {
							trans.Commit();
						} catch (SQLiteException e) {
							WeaveLogger.WriteMessage(e.Message, LogType.Error);
							throw new WeaveException("Database unavailable", 503);
						}
					}
				}
			}
		}

		private void AddParameters(SQLiteCommand cmd) {
			cmd.Parameters.Add(new SQLiteParameter("@userid", DbType.Int32));
			cmd.Parameters.Add(new SQLiteParameter("@collection", DbType.Int16));
			cmd.Parameters.Add(new SQLiteParameter("@id", DbType.String));
			cmd.Parameters.Add(new SQLiteParameter("@parentid", DbType.String));
			cmd.Parameters.Add(new SQLiteParameter("@predecessorid", DbType.String));
			cmd.Parameters.Add(new SQLiteParameter("@sortindex", DbType.Int32));
			cmd.Parameters.Add(new SQLiteParameter("@modified", DbType.Double));
			cmd.Parameters.Add(new SQLiteParameter("@payload", DbType.String));
			cmd.Parameters.Add(new SQLiteParameter("@payloadsize", DbType.Int32));
		}

		private void StoreWbo(SQLiteCommand cmd, WeaveBasicObject wbo) {
			cmd.Parameters["@userid"].Value = UserId;
			cmd.Parameters["@collection"].Value = WeaveCollectionDictionary.GetKey(wbo.Collection);
			cmd.Parameters["@id"].Value = wbo.Id;
			cmd.Parameters["@parentid"].Value = wbo.ParentId;
			cmd.Parameters["@predecessorid"].Value = wbo.PredecessorId;
			cmd.Parameters["@payload"].Value = wbo.Payload;
			cmd.Parameters["@payloadsize"].Value = wbo.PayloadSize();

			if (wbo.SortIndex.HasValue) {
				cmd.Parameters["@sortindex"].Value = wbo.SortIndex.Value;
			}
			if (wbo.Modified.HasValue) {
				cmd.Parameters["@modified"].Value = wbo.Modified.Value;
			}

			cmd.CommandText =
				@"REPLACE INTO Wbos (UserId, Id, Collection, ParentId, PredecessorId, SortIndex, Modified, Payload, PayloadSize) 
				  VALUES (@userid, @id, @collection, @parentid, @predecessorid, @sortindex, @modified, @payload, @payloadsize)";

			cmd.ExecuteNonQuery();
		}

		private void UpdateWbo(SQLiteCommand cmd, WeaveBasicObject wbo) {
			StringBuilder sb = new StringBuilder();

			cmd.Parameters["@userid"].Value = UserId;
			cmd.Parameters["@collection"].Value = WeaveCollectionDictionary.GetKey(wbo.Collection);
			cmd.Parameters["@id"].Value = wbo.Id;

			if (wbo.ParentId != null) {
				sb.Append("ParentId = @parentid").Append(",");
				cmd.Parameters["@parentid"].Value = wbo.ParentId;
			}

			if (wbo.PredecessorId != null) {
				sb.Append("PredecessorId = @predecessorid").Append(",");
				cmd.Parameters["@predecessorid"].Value = wbo.PredecessorId;
			}

			if (wbo.SortIndex.HasValue) {
				sb.Append("SortIndex = @sortindex").Append(",");
				cmd.Parameters.Add("@sortindex", DbType.Int32).Value = wbo.SortIndex.Value;
			}

			if (wbo.ParentId != null) {
				if (wbo.Modified.HasValue) {
					sb.Append("Modified = @modified").Append(",");
					cmd.Parameters["@modified"].Value = wbo.Modified.Value;
				} else {
					WeaveLogger.WriteMessage("Called UpdateWbo with no defined timestamp.", LogType.Error);
				}
			}

			if (sb.Length != 0) {
				sb.Insert(0, "UPDATE Wbo SET ");
				sb.Remove(sb.Length - 1, 1);
				sb.Append(" WHERE UserName = @username AND Wbo.Collection = @collection AND Wbo.Id = @id");

				cmd.CommandText = sb.ToString();

				cmd.ExecuteNonQuery();
			}
		}

		public void DeleteWbo(string id, string collection) {
			using (SQLiteConnection conn = new SQLiteConnection(ConnString))
			using (SQLiteCommand cmd = new SQLiteCommand(@"DELETE FROM Wbos
														   WHERE UserId = @userid
														   AND Collection = @collection 
														   AND Id = @id", conn)) {
				try {
					cmd.Parameters.Add("@userid", DbType.Int32).Value = UserId;
					cmd.Parameters.Add("@id", DbType.String).Value = id;
					cmd.Parameters.Add("@collection", DbType.Int16).Value = WeaveCollectionDictionary.GetKey(collection);

					conn.Open();
					cmd.ExecuteNonQuery();
				} catch (SQLiteException x) {
					WeaveLogger.WriteMessage(x.Message, LogType.Error);
					throw new WeaveException("Database unavailable", 503);
				}
			}
		}

		public void DeleteWboList(string collection, string id, string parentId, string predecessorId, string newer, string older, string sort,
								 string limit, string offset, string ids, string indexAbove, string indexBelow) {
			StringBuilder sb = new StringBuilder();

			using (SQLiteConnection conn = new SQLiteConnection(ConnString))
			using (SQLiteCommand cmd = new SQLiteCommand(conn)) {
				try {
					sb.Append(@"DELETE FROM Wbos 
								WHERE UserId = @userid
								AND Collection = @collection");
					cmd.Parameters.Add("@userid", DbType.Int32).Value = UserId;
					cmd.Parameters.Add("@collection", DbType.Int16).Value = WeaveCollectionDictionary.GetKey(collection); 

					if (limit != null || offset != null || sort != null) {
						IList<WeaveBasicObject> wboList = RetrieveWboList(collection, id, false, parentId, predecessorId, newer, older, sort, 
																		  limit, offset, ids, indexAbove, indexBelow);
						if (wboList.Count > 0) {
							sb.Append(" AND Id IN (");
							for (int x = 0; x < wboList.Count; x++) {
								cmd.Parameters.Add("@id" + x, DbType.String).Value = wboList[x].Id;
								sb.Append("@id" + x).Append(",");
							}

							sb.Remove(sb.Length - 1, 1).Append(")");
						}
					} else {
						if (id != null) {
							sb.Append(" AND Id = @id");
							cmd.Parameters.Add("@id", DbType.String).Value = id;
						}

						if (ids != null) {
							sb.Append(" AND Id IN (");
							string[] idArray = ids.Split(new[] { ',' });
							for (int x = 0; x < idArray.Length; x++) {
								cmd.Parameters.Add("@id" + x, DbType.String).Value = idArray[x];
								sb.Append("@id" + x).Append(",");
							}

							sb.Remove(sb.Length - 1, 1);
							sb.Append(")");
						}

						if (parentId != null) {
							sb.Append(" AND ParentId = @parentid");
							cmd.Parameters.Add("@parentid", DbType.String).Value = parentId;
						}

						if (predecessorId != null) {
							sb.Append(" AND PredecessorId = @predecessorid");
							cmd.Parameters.Add("@predecessorid", DbType.String).Value = predecessorId;
						}

						if (indexAbove != null) {
							sb.Append("AND SortIndex > @indexabove");
							cmd.Parameters.Add("@indexabove", DbType.Int32).Value = Convert.ToInt32(indexAbove);
						}

						if (indexBelow != null) {
							sb.Append(" AND SortIndex < @indexbelow");
							cmd.Parameters.Add("@indexbelow", DbType.Int32).Value = Convert.ToInt32(indexBelow);
						}

						if (newer != null) {
							sb.Append(" AND Modified > @newer");
							cmd.Parameters.Add("@newer", DbType.Double).Value = Convert.ToDouble(newer);
						}

						if (older != null) {
							sb.Append(" AND Modified < @older");
							cmd.Parameters.Add("@older", DbType.Double).Value = Convert.ToDouble(older);
						}
					}

					cmd.CommandText = sb.ToString();

					conn.Open();

					cmd.ExecuteNonQuery();
				} catch (SQLiteException x) {
					WeaveLogger.WriteMessage(x.Message, LogType.Error);
					throw new WeaveException("Database unavailable", 503);
				}
			}
		}

		public WeaveBasicObject RetrieveWbo(string collection, string id) {
			WeaveBasicObject wbo = new WeaveBasicObject();
			Dictionary<string, object> dic = new Dictionary<string, object>();

			using (SQLiteConnection conn = new SQLiteConnection(ConnString))
			using (SQLiteCommand cmd = new SQLiteCommand(@"SELECT * FROM Wbos 
														   WHERE UserId = @userid 
														   AND Collection = @collection 
														   AND Id = @id", conn)) {
				try {
					cmd.Parameters.Add("@userid", DbType.Int32).Value = UserId;
					cmd.Parameters.Add("@collection", DbType.Int16).Value = WeaveCollectionDictionary.GetKey(collection); 
					cmd.Parameters.Add("@id", DbType.String).Value = id; 

					conn.Open();
					using (SQLiteDataReader reader = cmd.ExecuteReader()) {
						if (reader.HasRows) {
							while (reader.Read()) {
								dic.Add("id", id);
								dic.Add("collection", collection);
								dic.Add("parentid", reader["ParentId"]);
								dic.Add("modified", reader["Modified"]);
								dic.Add("predecessorid", reader["PredecessorId"]);
								dic.Add("sortindex", reader["SortIndex"]);
								dic.Add("payload", reader["Payload"]);
							}
						}
					}
				} catch (SQLiteException x) {
					WeaveLogger.WriteMessage(x.Message, LogType.Error);
					throw new WeaveException("Database unavailable", 503);
				}
			}

			if (!wbo.Populate(dic)) {
				throw new WeaveException(WeaveErrorCodes.InvalidWbo + "", 400);
			}

			return wbo;
		}

		public IList<WeaveBasicObject> RetrieveWboList(string collection, string id, bool full, string parentId, 
													  string predecessorId, string newer, string older, string sort, string limit, string offset,
													  string ids, string indexAbove, string indexBelow) {           
			IList<WeaveBasicObject> wboList = new List<WeaveBasicObject>();
			StringBuilder sb = new StringBuilder();

			using (SQLiteConnection conn = new SQLiteConnection(ConnString))
			using (SQLiteCommand cmd = new SQLiteCommand(conn)) {
				try {
					sb.Append("SELECT ").Append(full ? "*" : "Id").Append(" FROM Wbos WHERE UserId = @userid AND Collection = @collection");

					cmd.Parameters.Add("@userid", DbType.Int32).Value = UserId;
					cmd.Parameters.Add("@collection", DbType.Int16).Value = WeaveCollectionDictionary.GetKey(collection); 
 
					if (id != null) {
						sb.Append(" AND Id = @id");
						cmd.Parameters.Add("@id", DbType.String).Value = id; 
					}

					if (ids != null) {
						sb.Append(" AND Id IN (");
						string[] idArray = ids.Split(new[] { ',' });
						for (int x = 0; x < idArray.Length; x++) {
							cmd.Parameters.Add("@id" + x, DbType.String).Value = idArray[x];
							sb.Append("@id" + x).Append(",");
						}

						sb.Remove(sb.Length - 1, 1);
						sb.Append(")");
					}

					if (parentId != null) {
						sb.Append(" AND ParentId = @parentid");
						cmd.Parameters.Add("@parentid", DbType.String).Value = parentId;
					}

					if (predecessorId != null) {
						sb.Append(" AND PredecessorId = @predecessorid");
						cmd.Parameters.Add("@predecessorid", DbType.String).Value = predecessorId;
					}

					if (indexAbove != null) {
						sb.Append("AND SortIndex > @indexabove");
						cmd.Parameters.Add("@indexabove", DbType.Int32).Value = Convert.ToInt32(indexAbove);
					}

					if (indexBelow != null) {
						sb.Append(" AND SortIndex < @indexbelow");
						cmd.Parameters.Add("@indexbelow", DbType.Int32).Value = Convert.ToInt32(indexBelow);
					}

					if (newer != null) {
						sb.Append(" AND Modified > @newer");
						cmd.Parameters.Add("@newer", DbType.Double).Value = Convert.ToDouble(newer);
					}

					if (older != null) {
						sb.Append(" AND Modified < @older");
						cmd.Parameters.Add("@older", DbType.Double).Value = Convert.ToDouble(older);
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

					if (limit != null) {
						sb.Append(" LIMIT ").Append(limit);
						if (offset != null) {
							sb.Append(" OFFSET ").Append(offset);
						}
					}

					cmd.CommandText = sb.ToString();

					conn.Open();

					using (SQLiteDataReader reader = cmd.ExecuteReader()) {
						if (reader.HasRows) {
							while (reader.Read()) {
								WeaveBasicObject wbo = new WeaveBasicObject();
								Dictionary<string, object> dic = new Dictionary<string, object>();

								if (full) {
									dic.Add("id", reader["Id"]);
									dic.Add("collection", collection);
									dic.Add("parentid", reader["ParentId"]);
									dic.Add("modified", reader["Modified"]);
									dic.Add("predecessorid", reader["PredecessorId"]);
									dic.Add("sortindex", reader["SortIndex"]);
									dic.Add("payload", reader["Payload"]);
								}

								wbo.Populate(dic);
								wboList.Add(wbo);
							}
						}
					}
				} catch (SQLiteException x) {
					WeaveLogger.WriteMessage(x.Message, LogType.Error);
					throw new WeaveException("Database unavailable", 503);
				}
			}

			return wboList;
		}

		public double GetStorageTotal() {
			return GetStorageTotal(UserName);
		}

		public bool AuthenticateUser(string password) {
			return AuthenticateUser(UserName, password);
		}

		public void ChangePassword(string password) {
			if (String.IsNullOrEmpty(password)) {
				throw new WeaveException("3", 404);
			}

			using (SQLiteConnection conn = new SQLiteConnection(ConnString))
			using (SQLiteCommand cmd = new SQLiteCommand(@"UPDATE Users SET Md5 = @md5 WHERE UserName = @username", conn)) {
				try {
					cmd.Parameters.Add("@username", DbType.String).Value = UserName;
					cmd.Parameters.Add("@md5", DbType.String).Value = HashString(password);

					conn.Open();
					cmd.ExecuteNonQuery();
				} catch (SQLiteException x) {
					WeaveLogger.WriteMessage(x.Message, LogType.Error);
					throw new WeaveException("Database unavailable", 503);
				}
			}
		}
	}
}