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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace WeaveCore {
    public class UserResult {
        public long UserId { get; set; }
        public string UserName { get; set; }
        public string Payload { get; set; }
        public double DateMin { get; set; }
        public double DateMax { get; set; }
    }

    public class UserDetailResult {
        public string Collection { get; set; }
        public long Count { get; set; }
        public string Payload { get; set; }
    }

    public class WeaveResultList {
        Dictionary<string, object> _result = new Dictionary<string, object>();
        Collection<string> _successIds;
        Dictionary<string, Collection<string>> _failedIds;
        double _modified;

        public WeaveResultList(double modified) {
            _modified = modified;
        }

        public Collection<string> SuccessIds {
            get {
                if (_successIds == null) {
                    _successIds = new Collection<string>();
                }

                return _successIds;
            }
        }

        public Dictionary<string, Collection<string>> FailedIds {
            get {
                if (_failedIds == null) {
                    _failedIds = new Dictionary<string, Collection<string>>();
                }

                return _failedIds;
            }
        }

        public string ToJson() {
            _result.Add("modified", _modified);
            _result.Add("success", SuccessIds);
            _result.Add("failed", FailedIds);

            return JsonConvert.SerializeObject(_result);
        }
    }
}