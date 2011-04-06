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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Web.Script.Serialization;
using Weave.Models;

namespace Weave {
    class WeaveBasicObject {
        Collection<string> _error = new Collection<string>();
        JavaScriptSerializer _jss = new JavaScriptSerializer();

        public string Id { get; set; }
        public string Collection { get; set; }
        public double? Modified { get; set; }
        public string Payload { get; set; }
        public int? SortIndex { get; set; }

        public int PayloadSize() {
            return (String.IsNullOrEmpty(Payload)) ? 0 : Encoding.ASCII.GetByteCount(Payload);
        }

        public bool Populate(string json) {
            if (json == null) {
                _error.Add("Json is null");
                return false;
            }

            Dictionary<string, object> dic = (Dictionary<string, object>)_jss.DeserializeObject(json);

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

            if (PayloadSize() > 262144) {
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

            return _jss.Serialize(dic);
        }

        public Collection<string> GetError() {
            return _error;
        }

        public void ClearError() {
            _error = new Collection<string>();
        }

        public Wbo GetModelWbo() {
            Wbo wbo = new Wbo();

            wbo.Collection = WeaveCollectionDictionary.GetKey(Collection);
            wbo.Id = Id;
            wbo.Modified = Modified;
            wbo.Payload = Payload;
            wbo.PayloadSize = PayloadSize();
            wbo.SortIndex = SortIndex;

            return wbo;
        }
    }
}
