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

using System;
using System.Collections.Specialized;
using System.IO;
using System.Text;

namespace WeaveCore {
    class WeaveRequest {
        public double RequestTime { get; private set; }

        private string _loginName;
        public string Password { get; private set; }

        public string Url { get; private set; }

        public string Version { get; private set; }
        public string UserName { get; private set; }
        public string PathName { get; private set; }
        public RequestFunction Function { get; private set; }
        public string Id { get; private set; }
        public string Collection { get; private set; }

        public RequestMethod RequestMethod { get; private set; }
        public NameValueCollection ServerVariables { get; private set; }
        public NameValueCollection QueryString { get; private set; }
        public double? HttpX { get; private set; }
        public string Content { get; private set; }
        public bool IsValid { get; private set; }
        public WeaveErrorCodes ErrorMessage { get; private set; }
        public int ErrorCode { get; private set; }

        public WeaveRequest(Uri url, NameValueCollection serverVariables, NameValueCollection queryString, Stream inputStream) {
            if (serverVariables == null || serverVariables.Count == 0 || String.IsNullOrEmpty(url.AbsolutePath)) {
                IsValid = false;
                return;
            }

            string baseUrl = url.AbsoluteUri;
            if (url.AbsolutePath.Length > 1) {
                baseUrl = baseUrl.Substring(0, baseUrl.IndexOf(url.AbsolutePath) + 1);
            }
            Url = baseUrl;

            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);
            RequestTime = Math.Round(ts.TotalSeconds, 2); 

            ServerVariables = serverVariables;
            QueryString = queryString;
            RequestMethod = (RequestMethod)Enum.Parse(typeof(RequestMethod), ServerVariables["REQUEST_METHOD"]);

            GetAuthenticationInfo(ServerVariables["HTTP_AUTHORIZATION"]);

            ProcessUrl(url.AbsolutePath);

            if ((RequestMethod == RequestMethod.POST || RequestMethod == RequestMethod.PUT) && inputStream != null && inputStream.Length != 0) {
                GetContent(inputStream);
            }

            if (ServerVariables["HTTP_X_IF_UNMODIFIED_SINCE"] != null) {
                HttpX = Math.Round(Convert.ToDouble(ServerVariables["HTTP_X_IF_UNMODIFIED_SINCE"]), 2);
            }

            if (IsValid) {
                Validate();
            }
        }

        private void ProcessUrl(string rawUrl) {
            int end = rawUrl.ToLower().IndexOf('?');
            if (end > -1) {
                rawUrl = rawUrl.Substring(0, end);
            }
            if (rawUrl.StartsWith("/")) {
                rawUrl = rawUrl.Substring(1, rawUrl.Length - 1);
            }

            string[] path = rawUrl.Split(new[] { '/' });
            
            if (path.Length < 3 || path.Length > 6) {
                IsValid = false;
                return;
            }

            int offset = 0;
            if (path[0] == "user") {
                PathName = path[0];
            } else {
                offset = -1;
            }

            Version = path[1 + offset];
            UserName = path[2 + offset];

            if (path.Length > 3 + offset) {
                RequestFunction requestFunction;
                Function = Enum.TryParse(path[3 + offset], true, out requestFunction) ? requestFunction : RequestFunction.NotSupported;
            }
            if (path.Length > 4 + offset) {
                Collection = path[4 + offset];
            }
            if (path.Length > 5 + offset) {
                Id = path[5 + offset];
            }

            IsValid = true;
        }

        private void GetAuthenticationInfo(string authHeader) {
            if (!string.IsNullOrEmpty(authHeader)) {
                if (authHeader.StartsWith("basic ", StringComparison.InvariantCultureIgnoreCase)) {
                    byte[] bytes = Convert.FromBase64String(authHeader.Substring(6));
                    string s = new ASCIIEncoding().GetString(bytes);

                    string[] parts = s.Split(new[] { ':' });
                    if (parts.Length == 2) {
                        _loginName = parts[0].ToLower();
                        Password = parts[1];
                    }
                }
            }         
        }

        private void GetContent(Stream inputStream ) {
            if (inputStream != null) {
                try {
                    using (StreamReader sr = new StreamReader(inputStream)) {
                        sr.BaseStream.Position = 0;
                        Content = sr.ReadToEnd();
                    }
                }
                catch (IOException) {
                    Content = null;
                }
            } else {
                Content = null;
            }
        }

        private void Validate() {
            if (Version != "1.0" && Version != "1.1") {
                ErrorMessage = WeaveErrorCodes.FunctionNotSupported;
                ErrorCode = 404;
                IsValid = false;
            } else if (Function == RequestFunction.Password && RequestMethod != RequestMethod.POST) {
                ErrorMessage = WeaveErrorCodes.InvalidProtocol;
                ErrorCode = 400;
                IsValid = false;
            } else if (Function == RequestFunction.Info && RequestMethod != RequestMethod.GET) {
                ErrorMessage = WeaveErrorCodes.InvalidProtocol;
                ErrorCode = 400;
                IsValid = false;
            } else if ((RequestMethod == RequestMethod.POST || RequestMethod == RequestMethod.PUT) && String.IsNullOrEmpty(Content)) {
                ErrorMessage = WeaveErrorCodes.InvalidProtocol;
                ErrorCode = 400;
                IsValid = false;
            } else if (PathName != "user") {
                if (Function == RequestFunction.NotSupported) {
                    ErrorMessage = WeaveErrorCodes.FunctionNotSupported;
                    ErrorCode = 400;
                    IsValid = false;
                } else if (String.IsNullOrEmpty(Password)) {
                    ErrorMessage = WeaveErrorCodes.MissingPassword;
                    ErrorCode = 401;
                    IsValid = false;
                } else if (UserName != _loginName) {
                    ErrorMessage = WeaveErrorCodes.UseridPathMismatch;
                    ErrorCode = 401;
                    IsValid = false;
                }
            }
        }
    }
}