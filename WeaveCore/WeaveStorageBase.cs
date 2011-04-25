/* Copyright (C) 2011 Karoly Lukacs <lukamicoder@gmail.com>
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
using System.Configuration;
using System.Data;
using System.Data.EntityClient;
using System.Data.Objects;
using System.Data.SQLite;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using WeaveCore.Models;

namespace WeaveCore {
	abstract class WeaveStorageBase {
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

			if (dbFile.Exists && dbFile.Length == 0) {
				dbFile.Delete();
				dbFile = new FileInfo(dbName);
			}

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
						Modified double NULL,
						SortIndex int NULL,
						Payload text NULL,
						PayloadSize int NULL,
						PRIMARY KEY (UserId, Collection, Id),
						FOREIGN KEY (UserId) REFERENCES Users(UserId));
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

                context.Users.MergeOption = MergeOption.NoTracking;

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

		public bool DeleteUser(Int64 userId) {
			bool result = false;

			using (WeaveContext context = new WeaveContext(ConnectionString)) {
				try {
					var wboList = (from wbos in context.Wbos
								   join users in context.Users on wbos.UserId equals users.UserId
								   where users.UserId == userId
								   select wbos).ToList();

					foreach (var del in wboList) {
						context.DeleteObject(del);
					}

					var user = (from u in context.Users
								where u.UserId == userId
								select u).SingleOrDefault();

					if (user != null) {
						context.DeleteObject(user);
					}

					int x = context.SaveChanges();

					if (x != 0) {
						result = true;
					}
				} catch (EntityException x) {
					WeaveLogger.WriteMessage(x.Message, LogType.Error);
					throw new WeaveException("Database unavailable.", 503);
				}
			}

			return result;
		}
	}
}