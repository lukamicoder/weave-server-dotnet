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
using System.Web.Mvc;
using NLog;
using WeaveCore;
using WeaveCore.Models;

namespace WeaveServer.Controllers {
    public class WeaveController : Controller {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public ContentResult Index() {
            var weave = new Weave();
            weave.LogEvent += OnLogEvent;

            weave.QuerySegments = Request.QueryString;
            weave.Headers = Request.Headers;
            weave.Body = GetContent();

            RequestMethod method;
            Enum.TryParse(Request.HttpMethod, out method);

            var response = weave.ProcessRequest(Request.Url, method);

            if (response.Response != null && response.Headers != null && response.Headers.Count > 0) {
                foreach (var pair in response.Headers) {
                    Response.AppendHeader(pair.Key, pair.Value);
                }
            }

            if (!String.IsNullOrEmpty(response.ErrorStatus)) {
                Response.Status = response.ErrorStatus;
                Response.StatusCode = response.ErrorStatusCode;
            }

            return Content(response.Response);
        }

        protected string GetContent() {
            if (Request.InputStream == null) {
                return null;
            }

            var buffer = new byte[Request.InputStream.Length];

            if (buffer.Length == 0) {
                return null;
            }

            Request.InputStream.Read(buffer, 0, Convert.ToInt32(Request.InputStream.Length));

            return System.Text.Encoding.Default.GetString(buffer);
        }

        private void OnLogEvent(object source, LogEventArgs args) {
            var level = LogLevel.Off;
            switch (args.Type) {
                case LogType.Error:
                    level = LogLevel.Error;
                    break;
                case LogType.Info:
                    level = LogLevel.Info;
                    break;
                case LogType.Warning:
                    level = LogLevel.Warn;
                    break;
                case LogType.Debug:
                    level = LogLevel.Debug;
                    break;
            }

            _logger.Log(level, args.Message);
        }
    }
}