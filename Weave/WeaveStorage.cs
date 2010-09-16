﻿/* Copyright (C) 2010 Karoly Lukacs <lukamicoder@gmail.com>
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
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Weave {
	abstract class WeaveStorage {
		public string ConnString { get; private set; }

		protected void SetupDatabase() {
			string dir = AppDomain.CurrentDomain.GetData("DataDirectory") as string;

			if (string.IsNullOrEmpty(dir)) {
				dir = AppDomain.CurrentDomain.BaseDirectory;
			}

			string dbName = dir + Path.DirectorySeparatorChar + "weave_db";

			ConnString = "Data Source=" + dbName + ";";

			FileInfo dbFile = new FileInfo(dbName);
			if (!dbFile.Exists) {
				try {
					SQLiteConnection.CreateFile(dbName);
				} catch (SQLiteException x) {
					WeaveLogger.WriteMessage(x.Message, LogType.Error);
					throw new WeaveException("Database unavailable", 503);
				}

				CreateTables();
			}
		}

		private void CreateTables() {
			const string createStatement = @"
				  BEGIN TRANSACTION;
				  CREATE TABLE Wbo (UserName text,
									Id text,
									Collection int,
									ParentId text,
									PredecessorId int,
									Modified real,
									SortIndex int,
									Payload text,
									PayloadSize int,
									primary key (UserName, Collection, Id));				
				  CREATE TABLE Users (UserName text, Md5 text, primary key (UserName));
				  CREATE INDEX parentindex ON Wbo (UserName, ParentId);
				  CREATE INDEX predecessorindex ON Wbo (UserName, PredecessorId);
				  CREATE INDEX modifiedindex ON Wbo (UserName, Collection, Modified);
				  END TRANSACTION;";

			using (SQLiteConnection conn = new SQLiteConnection(ConnString))
			using (SQLiteCommand cmd = new SQLiteCommand(createStatement, conn)) {
				try {
					conn.Open();
					cmd.ExecuteNonQuery();
				} catch (SQLiteException x) {
					WeaveLogger.WriteMessage(x.Message, LogType.Error);
					throw new WeaveException("Database unavailable", 503);
				}
			}
		}

		public bool AuthenticateUser(string userName, string password) {
			bool result = false;

			using (SQLiteConnection conn = new SQLiteConnection(ConnString))
			using (SQLiteCommand cmd = new SQLiteCommand(@"SELECT UserName FROM Users WHERE UserName = @username AND Md5 = @md5", conn)) {
				try {
					cmd.Parameters.Add("@username", DbType.String).Value = userName;
					cmd.Parameters.Add("@md5", DbType.String).Value = HashString(password);

					conn.Open();
					object obj = cmd.ExecuteScalar();
					if (obj != null) {
						result = true;
					}
				} catch (SQLiteException x) {
					WeaveLogger.WriteMessage(x.Message, LogType.Error);
					throw new WeaveException("Database unavailable", 503);
				}
			}

			return result;
		}

		public double GetStorageTotal(string userName) {
			double result = 0;
			using (SQLiteConnection conn = new SQLiteConnection(ConnString))
			using (SQLiteCommand cmd = new SQLiteCommand("SELECT ROUND(SUM(LENGTH(Payload))/1024) FROM Wbo WHERE UserName = @username", conn)) {
				try {
					cmd.Parameters.Add("@username", DbType.String).Value = userName;

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

			return result;
		}

		public Dictionary<string, long> GetCollectionListWithCounts(string userName) {
			var dic = new Dictionary<string, long>();

			using (SQLiteConnection conn = new SQLiteConnection(ConnString))
			using (SQLiteCommand cmd = new SQLiteCommand("SELECT Collection, COUNT(*) AS ct FROM Wbo WHERE UserName = @username GROUP BY Collection", conn)) {
				try {
					cmd.Parameters.Add("@username", DbType.String).Value = userName;

					conn.Open();
					using (SQLiteDataReader reader = cmd.ExecuteReader()) {
						if (reader.HasRows) {
							while (reader.Read()) {
								if (reader["Collection"] != DBNull.Value) {
									string coll = WeaveCollectionDictionary.GetValue((int)reader["Collection"]);
									dic.Add(coll, (long)reader["ct"]);
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

		public string HashString(string value) {
			StringBuilder hashedString = new StringBuilder();
			using (MD5CryptoServiceProvider serviceProvider = new MD5CryptoServiceProvider()) {
				byte[] data = serviceProvider.ComputeHash(Encoding.ASCII.GetBytes(value));
				for (int i = 0; i < data.Length; i++) {
					hashedString.Append(data[i].ToString("x2"));
				}
			}

			return hashedString.ToString();
		}
	}
}
