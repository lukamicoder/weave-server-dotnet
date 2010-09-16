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

using System.Collections.Generic;

namespace Weave {
    static class WeaveCollectionDictionary {
        private static Dictionary<int, string> _key = new Dictionary<int, string>
                                                      {
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

        public static Dictionary<int, string> Key {
            get { return _key; }
        }

        public static string GetValue(int key) {
            string value;
            _key.TryGetValue(key, out value);

            return value;
        }

        public static int GetKey(string value) {
            int key = 0;

            foreach (KeyValuePair<int, string> pair in _key) {
                if (pair.Value == value) {
                    key = pair.Key;
                    break;
                }
            }

            return key;
        }
    }
}
