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

using System.Text.RegularExpressions;

namespace Weave {
    static class WeaveValidation {
        static Regex _regex = new Regex(@"[^a-zA-Z0-9._-]");

        public static bool IsUserNameValid(string text) {
            if (string.IsNullOrEmpty(text) || text.Length > 32) {
                return false;
            }

            return !_regex.IsMatch(text);
        }
    }
}
