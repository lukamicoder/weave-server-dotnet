/* 
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

using System;
using System.ComponentModel.DataAnnotations;

namespace WeaveCore.Models {
    public class Wbo {
        [Key, Column(Order = 0)]
        public int UserId { get; set; }

        [Key, Column(Order = 1)]
        public string Id { get; set; }

        [Key, Column(Order = 2)]
        public Int16 Collection { get; set; }

        public Double? Modified { get; set; }
        public Int32? SortIndex { get; set; }

        [Column(TypeName = "ntext")]
        public string Payload { get; set; }
        
        public Int32? PayloadSize { get; set; }
        public Double Ttl { get; set; }

        public virtual User User { get; set; }
    }
}