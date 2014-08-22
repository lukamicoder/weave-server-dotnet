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

using System.Configuration;

namespace Weave.Core.Models {
	public class WeaveConfigurationSection : ConfigurationSection {
		[ConfigurationProperty("databaseType", DefaultValue = DatabaseType.SQLite, IsRequired = true)]
		public DatabaseType DatabaseType {
			get { return (DatabaseType)this["databaseType"]; }
			set { this["databaseType"] = value; }
		}

		[ConfigurationProperty("enableSsl", DefaultValue = false, IsRequired = true)]
		public bool EnableSsl {
			get { return (bool)this["enableSsl"]; }
			set { this["enableSsl"] = value; }
		}

		[ConfigurationProperty("enableAdminSsl", DefaultValue = false)]
		public bool EnableAdminSsl {
			get { return (bool)this["enableAdminSsl"]; }
			set { this["enableAdminSsl"] = value; }
		}

		[ConfigurationProperty("enableAdminService", DefaultValue = false, IsRequired = true)]
		public bool EnableAdminService {
			get { return (bool)this["enableAdminService"]; }
			set { this["enableAdminService"] = value; }
		}

		[ConfigurationProperty("port", IsRequired = true)]
		public long Port {
			get { return (long)this["port"]; }
			set { this["port"] = value; }
		}

		[ConfigurationProperty("adminPort")]
		public long AdminPort {
			get { return (long)this["adminPort"]; }
			set { this["adminPort"] = value; }
		}

		[ConfigurationProperty("enableDebug", DefaultValue = false, IsRequired = true)]
		public bool EnableDebug {
			get { return (bool)this["enableDebug"]; }
			set { this["enableDebug"] = value; }
		}

		[ConfigurationProperty("diagPassword", IsRequired = false)]
		public string DiagPassword {
			get { return (string)this["diagPassword"]; }
			set { this["diagPassword"] = value; }
		}

		[ConfigurationProperty("adminLogin", IsRequired = true)]
		public string AdminLogin {
			get { return (string)this["adminLogin"]; }
			set { this["adminLogin"] = value; }
		}

		[ConfigurationProperty("adminPassword", IsRequired = true)]
		public string AdminPassword {
			get { return (string)this["adminPassword"]; }
			set { this["adminPassword"] = value; }
		}


		[ConfigurationProperty("hmacPass", IsRequired = true)]
		public string HmacPass {
			get { return (string)this["hmacPass"]; }
			set { this["hmacPass"] = value; }
		}


		[ConfigurationProperty("rijndaelPass", IsRequired = true)]
		public string RijndaelPass {
			get { return (string)this["rijndaelPass"]; }
			set { this["rijndaelPass"] = value; }
		}
	}
}