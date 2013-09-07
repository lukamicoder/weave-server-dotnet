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

using System.Security.Cryptography;
using System.Text;
using Weave.Core.Models;

namespace Weave.Core {
    public abstract class WeaveBase {
        protected DBRepository DB;
        public delegate void WeaveLogEventHandler(object sender, LogEventArgs e);
        public event WeaveLogEventHandler LogEvent;

        protected void RaiseLogEvent(object source, string msg, LogType type) {
            OnLogEvent(source, new LogEventArgs(msg, type));
        }

        protected void OnLogEvent(object source, LogEventArgs args) {
            WeaveLogEventHandler tmp = LogEvent;
            if (tmp != null) {
                tmp(source, args);
            }
        }

        public static string ConvertToHash(string value) {
            var sb = new StringBuilder();
            using (var serviceProvider = new MD5CryptoServiceProvider()) {
                byte[] data = serviceProvider.ComputeHash(Encoding.ASCII.GetBytes(value));
                for (int i = 0; i < data.Length; i++) {
                    sb.Append(data[i].ToString("x2"));
                }
            }

            return sb.ToString();
        }
    }
}
