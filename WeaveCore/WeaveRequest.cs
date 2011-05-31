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
using System.Collections.Specialized;
using System.IO;
using System.Text;

namespace WeaveCore {
    class WeaveRequest {
        public double RequestTime { get; private set; }

        private string _loginName;
        public string Password { get; private set; }

        public string Version { get; private set; }
        public string UserName { get; private set; }
        public string Function { get; private set; }
        public string Id { get; private set; }
        public string Collection { get; private set; }

        public string RequestMethod { get; private set; }
        public NameValueCollection ServerVariables { get; private set; }
        public NameValueCollection QueryString { get; private set; }
        public double? HttpX { get; private set; }
        public string Content { get; private set; }
        public bool IsValid { get; private set; }
        public WeaveErrorCodes ErrorMessage { get; private set; }
        public int ErrorCode { get; private set; }

        public WeaveRequest(NameValueCollection serverVariables, NameValueCollection queryString, string rawUrl, Stream inputStream) {
            if (serverVariables == null || serverVariables.Count == 0 || String.IsNullOrEmpty(rawUrl)) {
                IsValid = false;
                return;
            }

            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);
            RequestTime = Math.Round(ts.TotalSeconds, 2); 

            ServerVariables = serverVariables;
            QueryString = queryString;
            RequestMethod = ServerVariables["REQUEST_METHOD"];

            GetAuthenticationInfo(ServerVariables["HTTP_AUTHORIZATION"]);    
      
            ProcessUrl(rawUrl);

            if ((RequestMethod == "POST" || RequestMethod == "PUT") && inputStream != null && inputStream.Length != 0) {
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
            string[] path = rawUrl.Split(new[] { '/' });

            switch (path.Length) {
                case 6:
                    Version = path[path.Length - 5];
                    UserName = path[path.Length - 4];
                    Function = path[path.Length - 3];
                    Collection = path[path.Length - 2];
                    Id = path[path.Length - 1];
                    IsValid = true;
                    break;
                case 5:
                    if (path[1] == "user") {
                        Version = path[path.Length - 3];
                        UserName = path[path.Length - 2];
                        Function = path[path.Length - 1];
                    } else {
                        Version = path[path.Length - 4];
                        UserName = path[path.Length - 3];
                        Function = path[path.Length - 2];
                        Collection = path[path.Length - 1];
                    }
                    IsValid = true;
                    break;
                default:
                    IsValid = false;
                    return;
            }         
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
            if (Version != "0.5" && Version != "1.0") {
                ErrorMessage = WeaveErrorCodes.FunctionNotSupported;
                ErrorCode = 404;
                IsValid = false;
            } else if (!WeaveValidation.IsUserNameValid(UserName)) {
                ErrorMessage = WeaveErrorCodes.InvalidUsername;
                ErrorCode = 400;
                IsValid = false;
            } else if (Function != "info" && Function != "storage" && Function != "password") {
                ErrorMessage = WeaveErrorCodes.FunctionNotSupported;
                ErrorCode = 400;
                IsValid = false;
            } else if (Function == "password" && RequestMethod != "POST") {
                ErrorMessage = WeaveErrorCodes.InvalidProtocol;
                ErrorCode = 400;
                IsValid = false;
            } else if (Function == "info" && RequestMethod != "GET") {
                ErrorMessage = WeaveErrorCodes.InvalidProtocol;
                ErrorCode = 400;
                IsValid = false;
            } else if (RequestMethod != "DELETE" && Function != "password" && !WeaveValidation.IsUserNameValid(Collection)) {
                ErrorMessage = WeaveErrorCodes.InvalidCollection;
                ErrorCode = 400;
                IsValid = false;
            } else if ((RequestMethod == "POST" || RequestMethod == "PUT") && String.IsNullOrEmpty(Content)) {
                ErrorMessage = WeaveErrorCodes.InvalidProtocol;
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