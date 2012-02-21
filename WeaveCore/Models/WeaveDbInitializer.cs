﻿/* 
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

using System.Data.Entity;

namespace WeaveCore.Models {
    class WeaveDbInitializer : DropCreateDatabaseIfModelChanges<WeaveContext> {
        protected override void Seed(WeaveContext context) {
            context.Database.ExecuteSqlCommand("CREATE INDEX Index_UserId_Collection_Modified ON Wbos (UserId, Collection, Modified)");
            context.Database.ExecuteSqlCommand("CREATE INDEX Index_Ttl ON Wbos (Ttl)");
        }
    }
}
