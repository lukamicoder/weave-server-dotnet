﻿using System.Configuration;

namespace WeaveCore {
    public class WeaveConfigurationSection : ConfigurationSection {
        [ConfigurationProperty("databaseType", DefaultValue = DatabaseType.SQLite, IsRequired = true)]
        public DatabaseType DatabaseType {
            get { return (DatabaseType)this["databaseType"]; }
            set { this["databaseType"] = value; }
        }
    }
}
