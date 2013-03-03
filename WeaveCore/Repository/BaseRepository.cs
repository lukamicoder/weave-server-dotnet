using System;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.Data.SqlClient;
using System.IO;
using Dapper;
using MySql.Data.MySqlClient;
using WeaveCore.Models;

namespace WeaveCore.Repository {
    public class BaseRepository {
        private readonly object _lock = new object();

        public static DatabaseType DatabaseType { get; set; }
        public static string ConnString { get; set; }

        protected void InitializeDatabase() {
            if (string.IsNullOrEmpty(ConnString) || DatabaseType == DatabaseType.NA) {
                DatabaseType = ((WeaveConfigurationSection)ConfigurationManager.GetSection("WeaveDatabase")).DatabaseType;

                if (DatabaseType == DatabaseType.SQLite) {
                    string dir = AppDomain.CurrentDomain.GetData("DataDirectory") as string;

                    if (string.IsNullOrEmpty(dir)) {
                        dir = AppDomain.CurrentDomain.BaseDirectory;
                    }

                    string dbName = dir + Path.DirectorySeparatorChar + "weave.db";

                    ConnString = "Data Source=" + dbName + ";";

                    lock (_lock) {
                        var dbFile = new FileInfo(dbName);
                        if (dbFile.Exists) {
                            return;
                        }

                        SQLiteConnection.CreateFile(dbName);
                        CreateSQLiteTables();
                    }
                } else {
                    var connections = ConfigurationManager.ConnectionStrings;
                    for (int x = 0; x < connections.Count; x++) {
                        if (connections[x].Name == "Weave") {
                            ConnString = connections[x].ConnectionString;
                            break;
                        }
                    }
                }
            }
        }

        private void CreateSQLiteTables() {
            const string sql = @"
				  BEGIN TRANSACTION;
				  CREATE TABLE Wbos (UserId integer NOT NULL,
									Id varchar(64) NOT NULL,
									Collection smallint NOT NULL,
									Modified double NULL,
									SortIndex integer NULL,
									Payload text NULL,
									PayloadSize integer NULL,
									Ttl double NULL,
									primary key (UserId, Collection, Id));				
				  CREATE TABLE Users (UserId integer NOT NULL PRIMARY KEY AUTOINCREMENT, 
									  UserName varchar(32) NOT NULL, 
									  Md5 varchar(128) NOT NULL, 
									  Email varchar(64) NULL);
				  CREATE INDEX modifiedindex ON Wbos (UserId, Collection, Modified);
				  END TRANSACTION;";

            using (var conn = GetConnection()) {
                conn.Execute(sql);
            }

        }

        protected IDbConnection GetConnection() {
            IDbConnection connection = null;

            switch (DatabaseType) {
                case DatabaseType.SQLite:
                    connection = new SQLiteConnection(ConnString);
                    connection.Open();
                    break;
                case DatabaseType.SQLServer:
                    connection = new SqlConnection(ConnString);
                    connection.Open();
                    break;
                case DatabaseType.MySQL:
                    connection = new MySqlConnection(ConnString);
                    connection.Open();
                    break;
            }

            return connection;
        }
    }
}
