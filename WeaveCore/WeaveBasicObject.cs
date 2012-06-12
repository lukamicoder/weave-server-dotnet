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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using ServiceStack.Text;
using WeaveCore.Models;

namespace WeaveCore {
    class WeaveBasicObject {
        Collection<string> _error = new Collection<string>();
        private double _ttl = 2100000000;

        public string Id { get; set; }
        public string Collection { get; set; }
        public double? Modified { get; set; }
        public string Payload { get; set; }
        public int? SortIndex { get; set; }

        public double Ttl {
            get { return _ttl; }
            set { _ttl = value;  }
        }

        public int? GetPayloadSize() {
            return (String.IsNullOrEmpty(Payload)) ? null : (int?)Encoding.ASCII.GetByteCount(Payload);
        }

        public bool Populate(string json) {
            if (json == null) {
                _error.Add("Json is null");
                return false;
            }

            var dic = JsonSerializer.DeserializeFromString<Dictionary<string, object>>(json);

            if (dic == null || dic.Count == 0) {
                _error.Add("Unable to extract from json");
                return false;
            }

            return Populate(dic);
        }

        public bool Populate(Dictionary<string, object> dic) {
            if (dic == null) {
                return false;
            }

            if (dic.ContainsKey("id") && dic["id"] != DBNull.Value) {
                Id = dic["id"] as string;
            }

            if (dic.ContainsKey("collection") && dic["collection"] != DBNull.Value) {
                Collection = dic["collection"] as string;
            }

            if (dic.ContainsKey("modified") && dic["modified"] != DBNull.Value) {
                try {
                    Modified = Convert.ToDouble(dic["modified"]);
                } catch (InvalidCastException) {
                    return false;
                }
            }

            if (dic.ContainsKey("sortindex") && dic["sortindex"] != DBNull.Value) {
                try {
                    SortIndex = Convert.ToInt32(dic["sortindex"]);
                } catch (InvalidCastException) {
                    return false;
                }
            }

            if (dic.ContainsKey("ttl") && dic["ttl"] != DBNull.Value) {
                try {
                    double tmpTtl = Convert.ToDouble(dic["ttl"]);
                    if (tmpTtl > 0 && tmpTtl < 31536000) {                      
                        TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);
                        double timeNow = Math.Round(ts.TotalSeconds, 2);
                        Ttl = timeNow + tmpTtl;
                    } else if (tmpTtl > 2100000000) {
                        return false;
                    }
                } catch (InvalidCastException) {
                    return false;
                }
            }

            if (dic.ContainsKey("payload") && dic["payload"] != DBNull.Value) {
                Payload = dic["payload"] as string;
            }

            return true;
        }

        public bool Validate() {
            if (Id == null || Id.Length > 64) {
                _error.Add("Invalid id");
            }

            if (!Modified.HasValue) {
                _error.Add("No modification date");
            }

            if (Collection == null || Collection.Length > 64) {
                _error.Add("Invalid collection");
            }

            if (SortIndex.HasValue && (SortIndex > 999999999 || SortIndex < -999999999)) {
                _error.Add("Invalid sortindex");
            }

            if (Ttl > 2100000000 || Ttl < 0) {
                _error.Add("Invalid expiration date");
            }

            if (GetPayloadSize() > 262144) {
                //Larger than 256KByte
                _error.Add("Payload too large");
            }

            if (_error.Count > 0) {
                return false;
            }

            return true;
        }

        public string ToJson() {
            var dic = new Dictionary<string, object>();

            if (Id != null) {
                dic.Add("id", Id);
            }

            if (Modified.HasValue) {
                dic.Add("modified", Modified.Value);
            }

            if (Payload != null) {
                dic.Add("payload", Payload);
            }

            if (SortIndex.HasValue) {
                dic.Add("sortindex", SortIndex.Value);
            }

            if (Collection != null) {
                dic.Add("collection", Collection);
            }

            dic.Add("ttl", Ttl);

            return JsonSerializer.SerializeToString(dic);
        }

        public Collection<string> GetError() {
            return _error;
        }

        public void ClearError() {
            _error = new Collection<string>();
        }

        public Wbo ToModelWbo() {
            Wbo wbo = new Wbo();

            wbo.Collection = WeaveCollectionDictionary.GetKey(Collection);
            wbo.Id = Id;
            wbo.Modified = Modified;
            wbo.Payload = Payload;
            wbo.PayloadSize = GetPayloadSize();
            wbo.SortIndex = SortIndex;
            wbo.Ttl = Ttl;

            return wbo;
        }
    }
}
