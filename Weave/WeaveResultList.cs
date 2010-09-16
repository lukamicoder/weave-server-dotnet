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
using System.Collections.ObjectModel;
using System.Web.Script.Serialization;

namespace Weave {
    class WeaveResultList {
        JavaScriptSerializer jss = new JavaScriptSerializer();
        Dictionary<string, object> result = new Dictionary<string, object>();
        Collection<string> successIds;
        Dictionary<string, Collection<string>> failedIds;

        public Collection<string> SuccessIds {
            get {
                if (successIds == null) {
                    successIds = new Collection<string>();
                }

                return successIds;
            }   
        }

        public Dictionary<string, Collection<string>> FailedIds {
            get {
                if (failedIds == null) {
                    failedIds = new Dictionary<string, Collection<string>>();
                }

                return failedIds;
            }
        }

        public string ToJson() {
            result.Add("success", successIds);
            result.Add("failed", failedIds);
            return jss.Serialize(result);
        }
    }
}
