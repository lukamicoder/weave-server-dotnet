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
using System.Configuration;
using System.Data.EntityClient;
using System.Data.SQLite;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using Weave.Models;

namespace Weave {
	abstract class WeaveStorage {
		private string _connString;

		public Int64 UserId { get; private set; }

		public string ConnectionString {
			get {
				if (String.IsNullOrEmpty(_connString)) {
					GetConnectionString();
				}

				return _connString;
			}
		}

		protected void GetConnectionString() {
			if (ConfigurationManager.ConnectionStrings["Weave"] != null) {
				var builder = new EntityConnectionStringBuilder();
				string provider = ConfigurationManager.ConnectionStrings["Weave"].ProviderName;

				if (provider.ToLower() == "system.data.sqlclient") {
					builder.Metadata = "res://*/Models.SQLServerModel.csdl|res://*/Models.SQLServerModel.ssdl|res://*/Models.SQLServerModel.msl";
					builder.Provider = provider;
					builder.ProviderConnectionString = ConfigurationManager.ConnectionStrings["Weave"].ConnectionString;
					_connString = builder.ToString();
				} else {
					SetupSQLiteDatabase();
				}
			} else {
				SetupSQLiteDatabase();
			}
		}

		protected void SetupSQLiteDatabase() {
			var builder = new EntityConnectionStringBuilder();
			builder.Metadata = "res://*/Models.SQLiteModel.csdl|res://*/Models.SQLiteModel.ssdl|res://*/Models.SQLiteModel.msl";
			builder.Provider = "System.Data.SQLite";

			string dir = AppDomain.CurrentDomain.GetData("DataDirectory") as string;

			if (string.IsNullOrEmpty(dir)) {
				dir = AppDomain.CurrentDomain.BaseDirectory;
			}

			string dbName = dir + Path.DirectorySeparatorChar + "Weave.db";

			builder.ProviderConnectionString = "Data Source=" + dbName + ";";

			_connString = builder.ToString();

			FileInfo dbFile = new FileInfo(dbName);
			if (!dbFile.Exists) {
				try {
					SQLiteConnection.CreateFile(dbName);
					CreateSQLiteTables();
				} catch (SQLiteException x) {
					WeaveLogger.WriteMessage(x.Message, LogType.Error);
					throw new WeaveException("Database unavailable.", 503);
				}
			}
		}

		private void CreateSQLiteTables() {
			const string cmd = @"
				  BEGIN TRANSACTION;				
				  CREATE TABLE Users (
						UserId integer NOT NULL PRIMARY KEY AUTOINCREMENT, 
						UserName varchar(32) NOT NULL, 
						Md5 varchar(32) NOT NULL);
				  CREATE TABLE Wbos (
						UserId integer NOT NULL,
						Id varchar(64) NOT NULL,
						Collection smallint NOT NULL,
						ParentId varchar(64) NULL,
						PredecessorId varchar(64) NULL,
						Modified double NULL,
						SortIndex int NULL,
						Payload text NULL,
						PayloadSize int NULL,
						PRIMARY KEY (UserId, Collection, Id),
						FOREIGN KEY (UserId) REFERENCES Users(UserId));
				  CREATE INDEX parentindex ON Wbos (UserId, ParentId);
				  CREATE INDEX predecessorindex ON Wbos (UserId, PredecessorId);
				  CREATE INDEX modifiedindex ON Wbos (UserId, Collection, Modified);
				  END TRANSACTION;";

			using (WeaveContext context = new WeaveContext(ConnectionString)) {
				context.ExecuteStoreCommand(cmd);
			}
		}

		public bool AuthenticateUser(string userName, string password) {
			bool result = false;

			using (WeaveContext context = new WeaveContext(ConnectionString)) {
				string hash = HashString(password);
				var id = (from u in context.Users
						  where u.UserName == userName && u.Md5 == hash
						  select u.UserId).SingleOrDefault();

				if (id != 0) {
					UserId = id;
					result = true;
				}
			}

			return result;
		}

		public double GetStorageTotal(Int64 userId) {
			double result;

			using (WeaveContext context = new WeaveContext(ConnectionString)) {
				var total = (from u in context.Users
							 where u.UserId == userId
							 join w in context.Wbos on u.UserId equals w.UserId
							 into g
							 select new {
								 Payload = (double?)g.Sum(p => p.PayloadSize)
							 }).SingleOrDefault();

				result = total.Payload.Value / 1024;
			}
			return result;
		}

		public Dictionary<string, long> GetCollectionListWithCounts(Int64 userId) {
			var dic = new Dictionary<string, long>();
			using (WeaveContext context = new WeaveContext(ConnectionString)) {
				var cts = from w in context.Wbos
						  where w.UserId == userId
						  group w by new { w.Collection } into g
						  select new { g.Key.Collection, ct = (Int64)g.Count() };

				foreach (var p in cts) {
					dic.Add(WeaveCollectionDictionary.GetValue(p.Collection), p.ct);
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