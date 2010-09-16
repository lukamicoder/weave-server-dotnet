﻿/* Copyright (C) 2010 Karoly Lukacs <lukamicoder@gmail.com>
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
using System.Web.Mvc;
using Weave;

namespace WeaveServer.Controllers {
    public class WeaveController : Controller {
        public string Index() {
            WeaveRequest request = new WeaveRequest(Request.ServerVariables, Request.QueryString, Request.RawUrl, Request.InputStream);
            WeaveUser weave = new WeaveUser(request);

            if (weave.Headers != null && weave.Headers.Count > 0) {
                foreach (KeyValuePair<string, string> pair in weave.Headers) {
                    Response.AppendHeader(pair.Key, pair.Value);
                }
            }

            if (!String.IsNullOrEmpty(weave.ErrorStatus)) {
                Response.Status = weave.ErrorStatus;
                Response.StatusCode = weave.ErrorStatusCode;
            }

            return weave.Response;
        }
    }
}