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
using NLog;
using Nancy;
using Nancy.ErrorHandling;
using Nancy.Extensions;

namespace Weave.Server {
    public class StatusCodeHandler : IStatusCodeHandler {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public bool HandlesStatusCode(HttpStatusCode statusCode, NancyContext context) {
            if (statusCode == HttpStatusCode.NotFound || 
                statusCode == HttpStatusCode.InternalServerError) {
                return true;
            }

            return false;
        }

        public void Handle(HttpStatusCode statusCode, NancyContext context) {
            switch (statusCode) {
                case HttpStatusCode.NotFound:
                    _logger.Error("Error 404. File not found: " + context.Request.Url);
                    break;
                case HttpStatusCode.InternalServerError:
                    _logger.Error("Error 500. Internal Server Error." + Environment.NewLine + context.GetExceptionDetails());
                    break;
            }

            context.Response = "";
            context.Response.StatusCode = statusCode;
        }
    }
}
