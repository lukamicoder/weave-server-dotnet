/* 
Weave Server.NET <http://code.google.com/p/weave-server-dotnet/>
Copyright (C) 2014 Karoly Lukacs

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

namespace Weave.Core.Models {
    public class WeaveRequest {
        public RequestMethod RequestMethod { get; set; }
        public long RequestTime { get; set; }

        public string LoginPassword { get; set; }
        public string LoginName { get; set; }
        public long UserId { get; set; }

        public string Url { get; set; }
        public string Version { get; set; }
        public string UserName { get; set; }
        public string PathName { get; set; }
        public RequestFunction Function { get; set; }
        public string Id { get; set; }
        public string Collection { get; set; }

        public double? HttpX { get; set; }
        public bool IsValid { get; set; }
        public WeaveErrorCodes ErrorMessage { get; set; }
        public int ErrorCode { get; set; }
    }
}