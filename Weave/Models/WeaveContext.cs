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

using System.Data.Objects;

namespace Weave.Models {
    class WeaveContext : ObjectContext {
        public ObjectSet<User> Users { get; private set; }
        public ObjectSet<Wbo> Wbos { get; private set; }

        public WeaveContext(string connectionString)
            : base(connectionString, "WeaveEntities") {
            Users = CreateObjectSet<User>();
            Wbos = CreateObjectSet<Wbo>(); 
            
            this.Connection.ConnectionString = connectionString;     
        }
    }
}