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

using System.Collections.Generic;
using System.Linq;

namespace WeaveCore.Models {
    static class CollectionDictionary {
        private static Dictionary<short, string> _key = new Dictionary<short, string>
                                                      {
                                                              {0, "addson"},
                                                              {1, "clients"},
                                                              {2, "crypto"},
                                                              {3, "forms"},
                                                              {4, "history"},
                                                              {5, "keys"},
                                                              {6, "meta"},
                                                              {7, "bookmarks"},
                                                              {8, "prefs"},
                                                              {9, "tabs"},
                                                              {10, "passwords"}
                                                          };

        public static Dictionary<short, string> Key {
            get { return _key; }
        }

        public static string GetValue(short key) {
            string value;
            _key.TryGetValue(key, out value);

            return value;
        }

        public static short GetKey(string value) {
            return (from pair in _key where pair.Value == value select pair.Key).FirstOrDefault();
        }
    }
}
