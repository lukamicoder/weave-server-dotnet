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
using System.Configuration;
using System.Text;
using NLog;
using Nancy;
using Nancy.ErrorHandling;
using Nancy.Extensions;

namespace Weave.Admin.Server {
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
            var sb = new StringBuilder();
            sb.Append(
@"<html>
    <head>
        <title>Error</title>
	    <style type='text/css'>
		    body	 { margin:50px 0px; font-size:24px; font-family:Tahoma, Arial, Helvetica, sans-serif; color:#333; }
		    .content { margin:0px auto; width:").Append(statusCode == HttpStatusCode.NotFound ? 3 : 6).Append(@"00px; padding:0 15px 0 15px; border:1px solid #ccc; background-color:#F7F8F3; text-align:center;	}			
		    .msg     { font-size:small; text-align:left; padding: 15px 0 15px 0; }
		    .line    { width:100%; border-top:1px solid #ccc; height:0px; }
	    </style>
    </head>
    <body>
        <div class='content'>");

            switch (statusCode) {
                case HttpStatusCode.NotFound:
                    sb.Append("<p>Error 404. File not found.</p>");

                    _logger.Error("Error 404. File not found: " + context.Request.Url);
                    break;
                case HttpStatusCode.InternalServerError:
                    sb.Append(@"<p>Error 500. Internal Server Error.</p>");

                    if (ConfigurationManager.AppSettings["EnableDebugging"].ToLower() == "true") {
                        sb.Append("<div class='line'></div><div class='msg'>");
                        sb.Append(context.GetExceptionDetails());
                    }

                    _logger.Error("Error 500. Internal Server Error." + Environment.NewLine + context.GetExceptionDetails());
                    break;
            }

            sb.Append(@"
        </div>
    </body>
</html>");

            context.Response = sb.ToString();
            context.Response.StatusCode = statusCode;
        }
    }
}
