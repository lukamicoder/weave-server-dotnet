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

using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace WeaveCore {
    static class WeaveHelper {
        public static bool IsUserNameValid(string text) {
            var regex = new Regex(@"[^a-zA-Z0-9._-]");
            
            if (string.IsNullOrEmpty(text) || text.Length > 32) {
                return false;
            }

            return !regex.IsMatch(text);
        }

        public static string ConvertToHash(string value) {
            StringBuilder sb = new StringBuilder();
            using (var serviceProvider = new MD5CryptoServiceProvider()) {
                byte[] data = serviceProvider.ComputeHash(Encoding.ASCII.GetBytes(value));
                for (int i = 0; i < data.Length; i++) {
                    sb.Append(data[i].ToString("x2"));
                }
            }

            return sb.ToString();
        }

        public static string FormatPayloadSize(decimal? amount) {
            if (amount == null) {
                return "";
            }
            string output = "";
      
            double total = (Convert.ToDouble(amount) * 1000) / 1024 / 1024;
            if (total >= 1024) {
                output = Math.Round((total / 1024), 1) + "MB";
            } else if (total >= 0) {
                output = Math.Round(total, 1) + "KB";
            }

            return output;
        }
    }
}
