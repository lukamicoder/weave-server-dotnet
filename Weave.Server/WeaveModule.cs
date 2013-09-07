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
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using NLog;
using Nancy;
using Nancy.Security;
using Weave.Core.Models;

namespace Weave.Server {
    public class WeaveModule : NancyModule {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public WeaveModule() {
#if !DEBUG
            if (ConfigurationManager.AppSettings["EnableSsl"].ToLower() == "true") {
                this.RequiresHttps();
            }
#endif

            const string route = "/{param1?}/{param2?}/{param3?}/{param4?}/{param5?}";

			Get[route] = parameters => GetWeaveResponse();
			Post[route] = parameters => GetWeaveResponse();
			Put[route] = parameters => GetWeaveResponse();
			Delete[route] = parameters => GetWeaveResponse();

            After += ctx => {
                if (ctx.Response.StatusCode == HttpStatusCode.NotFound || ctx.Response.StatusCode == HttpStatusCode.Unauthorized) {
                    var code = ctx.Response.StatusCode;
                    ctx.Response = "";
                    ctx.Response.StatusCode = code;
                }
            };
        }

		private Response GetWeaveResponse() {
			var weave = new Weave.Core.Weave();
			weave.LogEvent += OnLogEvent;

			var query = new NameValueCollection();
			foreach (var name in Request.Query.GetDynamicMemberNames()) {
				query.Add(name, Request.Query[name]);
			}
            weave.QuerySegments = query;

            var headers = new NameValueCollection();
            foreach (var header in Request.Headers) {
                if (header.Value != null) {
                    headers.Add(header.Key, header.Value.FirstOrDefault());
                }
            }
            weave.Headers = headers;

            RequestMethod method;
            Enum.TryParse(Request.Method, out method);

			weave.Body = GetContent();

			var response = weave.ProcessRequest(Request.Url, method);

			var nancyResponse = (Response)response.Response;

			if (response.Response != null && response.Headers != null && response.Headers.Count > 0) {
				foreach (var pair in response.Headers) {
					nancyResponse.Headers.Add(pair.Key, pair.Value);

                    if (pair.Key == "Content-type") {
                        nancyResponse.ContentType = pair.Value;
                    }
				}
			}

			if (!String.IsNullOrEmpty(response.ErrorStatus)) {
				nancyResponse.StatusCode = (HttpStatusCode)response.ErrorStatusCode;
				nancyResponse.ContentType = "text/plain";
				nancyResponse.Contents = stream => (new StreamWriter(stream) { AutoFlush = true }).Write(response.ErrorStatus);
			}

			return nancyResponse;
		}

		protected string GetContent() {
			var buffer = new byte[Request.Body.Length];

			if (buffer.Length == 0) {
				return null;
			}

			Request.Body.Read(buffer, 0, Convert.ToInt32(Request.Body.Length));

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