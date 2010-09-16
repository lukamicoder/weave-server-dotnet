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
using System.Data;
using System.Data.SQLite;

namespace Weave {
	class WeaveStorageAdmin : WeaveStorage {
		public WeaveStorageAdmin() {
			SetupDatabase();
		}

		public List<WeaveUserListItem> GetUserList() {
			List<WeaveUserListItem> result = new List<WeaveUserListItem>();
			string cmdString = @"SELECT Users.UserName AS User, ROUND(SUM(LENGTH(Wbo.Payload))/1024) AS Payload, MAX(Wbo.Modified) AS Date 
								 FROM Users
								 LEFT JOIN Wbo ON Users.UserName = Wbo.UserName
								 GROUP BY User";
			using (SQLiteConnection conn = new SQLiteConnection(ConnString))
			using (SQLiteCommand cmd = new SQLiteCommand(cmdString, conn)) {
				try {
					conn.Open();
					using (SQLiteDataReader reader = cmd.ExecuteReader()) {
						if (reader.HasRows) {
							while (reader.Read()) {
								WeaveUserListItem wu = new WeaveUserListItem();
								wu.User = (string) reader["User"];
								if (reader["Payload"] != DBNull.Value) {
									wu.Payload = ((double) reader["Payload"] * 1000) / 1024;
								}
								if (reader["Date"] != DBNull.Value) {
									wu.Date = 1000 * (double) reader["Date"];
								} else {
									wu.Date = 0;
								}
								result.Add(wu);
							}
						}
					}
				} catch (SQLiteException x) {
					WeaveLogger.WriteMessage(x.Message, LogType.Error);
					throw new WeaveException("Database unavailable", 503);
				}
			}

			return result;
		}

		public bool DeleteUser(string userName) {
			bool result = false;

			if (!String.IsNullOrEmpty(userName)) {
				string commandText = @"BEGIN TRANSACTION;
									   DELETE FROM Users WHERE UserName = @username;
									   DELETE FROM Wbo WHERE UserName = @username;
									   END TRANSACTION";

				using (SQLiteConnection conn = new SQLiteConnection(ConnString))
				using (SQLiteCommand cmd = new SQLiteCommand(commandText, conn)) {
					try {
						cmd.Parameters.Add("@username", DbType.String).Value = userName;

						conn.Open();
						cmd.ExecuteNonQuery();

						result = true;
					}
					catch (SQLiteException x) {
						WeaveLogger.WriteMessage(x.Message, LogType.Error);
					}
				}
			}

			return result;
		}

		public bool CreateUser(string userName, string password) {
			bool result = false;

			if (!String.IsNullOrEmpty(userName)) {
				using (SQLiteConnection conn = new SQLiteConnection(ConnString))
				using (SQLiteCommand cmd = new SQLiteCommand(@"INSERT INTO Users (UserName, Md5) VALUES (@username, @md5)", conn)) {
					try {
						cmd.Parameters.Add("@username", DbType.String).Value = userName;
						cmd.Parameters.Add("@md5", DbType.String).Value = HashString(password);

						conn.Open();
						cmd.ExecuteNonQuery();

						result = true;
					}
					catch (SQLiteException x) {
						WeaveLogger.WriteMessage(x.Message, LogType.Error);
					}
				}
			}

			return result;
		}

		public bool IsUniqueUserName(string userName) {
			bool result = false;

			using (SQLiteConnection conn = new SQLiteConnection(ConnString))
			using (SQLiteCommand cmd = new SQLiteCommand(@"SELECT UserName FROM Users WHERE UserName = @username", conn)) {
				try {
					cmd.Parameters.Add("@username", DbType.String).Value = userName;

					conn.Open();
					object obj = cmd.ExecuteScalar();
					if (obj == null) {
						result = true;
					}
				} catch (SQLiteException x) {
					WeaveLogger.WriteMessage(x.Message, LogType.Error);
				}
			}

			return result;
		}
	}	
}