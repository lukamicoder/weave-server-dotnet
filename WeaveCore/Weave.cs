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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text;
using Newtonsoft.Json;
using WeaveCore.Models;

namespace WeaveCore {
    public class Weave : LogEventBase {
        readonly DBRepository _db;
        private WeaveResponse _response;
        private WeaveRequest _request;

        public string Body { get; set; }
        public NameValueCollection QuerySegments { get; set; }
        public NameValueCollection Headers { get; set; }

        public Weave() {
            _db = new DBRepository();
        }

        public WeaveResponse ProcessRequest(Uri url, RequestMethod method) {
            _response = new WeaveResponse();
            _request = new WeaveRequest { RequestMethod = method };

            ParseRequest(url);

            _response.Headers = new Dictionary<string, string> { { "Content-type", "application/json" }, { "X-Weave-Timestamp", _request.RequestTime + "" } };

            if (!_request.IsValid) {
                if (_request.ErrorMessage != 0) {
                    _response.Response = SetError(_request.ErrorMessage, _request.ErrorCode);
                }

                return _response;
            }

            try {
                _request.UserId = _db.AuthenticateUser(_request.UserName, _request.LoginPassword);
                if (_request.UserId == 0) {
                    _response.Response = SetError("Authentication failed", 401);
                    return _response;
                }
            } catch (Exception x) {
                RaiseLogEvent(this, x.ToString(), LogType.Error);
                _response.Response = SetError("Database unavailable", 503);
                return _response;
            }

            switch (_request.RequestMethod) {
                case RequestMethod.GET:
                    switch (_request.Function) {
                        case RequestFunction.Info:
                            RequestGetInfo();
                            break;
                        case RequestFunction.Storage:
                            RequestGetStorage();
                            break;
                        case RequestFunction.Node:
                            _response.Response = _request.Url;
                            break;
                        default:
                            _response.Response = SetError(WeaveErrorCodes.InvalidProtocol, 400);
                            break;
                    }
                    break;
                case RequestMethod.PUT:
                    RequestPut();
                    break;
                case RequestMethod.POST:
                    RequestPost();
                    break;
                case RequestMethod.DELETE:
                    RequestDelete();
                    break;
                default:
                    _response.Response = SetError(WeaveErrorCodes.InvalidProtocol, 400);
                    break;
            }

            return _response;
        }

        #region Parse Request
        private void ParseRequest(Uri url) {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);
            _request.RequestTime = Convert.ToInt64(ts.TotalSeconds);

            if (String.IsNullOrEmpty(url.AbsolutePath)) {
                _request.IsValid = false;
                return;
            }

            string baseUrl = url.AbsoluteUri;
            if (url.AbsolutePath.Length > 1) {
                baseUrl = baseUrl.Substring(0, baseUrl.IndexOf(url.AbsolutePath) + 1);
            }
            _request.Url = baseUrl;

            GetAuthenticationInfo();

            ParseUrl(url.AbsolutePath);

            if (!String.IsNullOrEmpty(Headers["x-if-unmodified-since"])) {
                _request.HttpX = Math.Round(Convert.ToDouble(Headers["x-if-unmodified-since"]), 2);
            }

            if (_request.IsValid) {
                Validate();
            }

            if (_request.PathName == "user") {
                ProcessUser();
            }
        }

        private void ProcessUser() {
            if (_request.UserName == "a") {
                _response.Response = "0";
                _request.IsValid = false;
                return;
            }

            if (_request.UserName.Length != 32) {
                return;
            }

            var wa = new WeaveAdmin();
            if (_request.RequestMethod == RequestMethod.GET && _request.Function == RequestFunction.NotSupported) {
                _response.Response = wa.IsUserNameUnique(_request.UserName) ? "0" : "1";
                _request.IsValid = false;
                return;
            }

            if (_request.RequestMethod == RequestMethod.PUT) {
                var dic = JsonConvert.DeserializeObject<Dictionary<string, object>>(Body);

                if (dic == null || dic.Count == 0) {
                    _response.Response = SetError("Unable to extract from json", 400);
                    _request.IsValid = false;
                    return;
                }

                string output = wa.CreateUser(_request.UserName, (string)dic["password"], (string)dic["email"]);
                _response.Response = String.IsNullOrEmpty(output) ? _request.UserName : output;
                _request.IsValid = false;
            }
        }

        private void ParseUrl(string rawUrl) {
            int end = rawUrl.ToLower().IndexOf('?');
            if (end > -1) {
                rawUrl = rawUrl.Substring(0, end);
            }
            if (rawUrl.StartsWith("/")) {
                rawUrl = rawUrl.Substring(1, rawUrl.Length - 1);
            }

            string[] segments = rawUrl.Split(new[] { '/' });

            if (segments.Length < 3 || segments.Length > 6) {
                _request.IsValid = false;
                return;
            }

            int offset = 0;
            if (segments[0] == "user") {
                _request.PathName = segments[0];
            } else {
                offset = -1;
            }

            _request.Version = segments[1 + offset];
            _request.UserName = segments[2 + offset];

            if (segments.Length > 3 + offset) {
                RequestFunction requestFunction;
                _request.Function = Enum.TryParse(segments[3 + offset], true, out requestFunction) ? requestFunction : RequestFunction.NotSupported;
            }
            if (segments.Length > 4 + offset) {
                _request.Collection = segments[4 + offset];
            }
            if (segments.Length > 5 + offset) {
                _request.Id = segments[5 + offset];
            }

            _request.IsValid = true;
        }

        private void GetAuthenticationInfo() {
            string auth = Headers["authorization"];
            if (string.IsNullOrEmpty(auth) || !auth.StartsWith("basic ", StringComparison.InvariantCultureIgnoreCase)) {
                return;
            }

            byte[] bytes = Convert.FromBase64String(auth.Substring(6));
            string s = new ASCIIEncoding().GetString(bytes);

            string[] parts = s.Split(new[] { ':' });
            if (parts.Length == 2) {
                _request.LoginName = parts[0].ToLower();
                _request.LoginPassword = parts[1];
            }
        }

        private void Validate() {
            if (_request.Version != "1.0" && _request.Version != "1.1") {
                _request.ErrorMessage = WeaveErrorCodes.FunctionNotSupported;
                _request.ErrorCode = 404;
                _request.IsValid = false;
            } else if (_request.Function == RequestFunction.Password && _request.RequestMethod != RequestMethod.POST) {
                _request.ErrorMessage = WeaveErrorCodes.InvalidProtocol;
                _request.ErrorCode = 400;
                _request.IsValid = false;
            } else if (_request.Function == RequestFunction.Info && _request.RequestMethod != RequestMethod.GET) {
                _request.ErrorMessage = WeaveErrorCodes.InvalidProtocol;
                _request.ErrorCode = 400;
                _request.IsValid = false;
            } else if ((_request.RequestMethod == RequestMethod.POST || _request.RequestMethod == RequestMethod.PUT) && String.IsNullOrEmpty(Body)) {
                _request.ErrorMessage = WeaveErrorCodes.InvalidProtocol;
                _request.ErrorCode = 400;
                _request.IsValid = false;
            } else if (_request.PathName != "user") {
                if (_request.Function == RequestFunction.NotSupported) {
                    _request.ErrorMessage = WeaveErrorCodes.FunctionNotSupported;
                    _request.ErrorCode = 400;
                    _request.IsValid = false;
                } else if (String.IsNullOrEmpty(_request.LoginPassword)) {
                    _request.ErrorMessage = WeaveErrorCodes.MissingPassword;
                    _request.ErrorCode = 401;
                    _request.IsValid = false;
                } else if (_request.UserName != _request.LoginName) {
                    _request.ErrorMessage = WeaveErrorCodes.UseridPathMismatch;
                    _request.ErrorCode = 401;
                    _request.IsValid = false;
                }
            }
        }
        #endregion

        #region Process Request
        private void RequestGetInfo() {
            try {
                switch (_request.Collection) {
                    case "quota":
                        _response.Response = JsonConvert.SerializeObject(new[] { _db.GetStorageTotal(_request.UserId) });
                        break;
                    case "collections":
                        _response.Response = JsonConvert.SerializeObject(_db.GetCollectionListWithTimestamps(_request.UserId));
                        break;
                    case "collection_counts":
                        _response.Response = JsonConvert.SerializeObject(_db.GetCollectionListWithCounts(_request.UserId));
                        break;
                    case "collection_usage":
                        _response.Response = JsonConvert.SerializeObject(_db.GetCollectionStorageTotals(_request.UserId));
                        break;
                    default:
                        _response.Response = SetError(WeaveErrorCodes.InvalidProtocol, 400);
                        break;
                }
            } catch (Exception x) {
                RaiseLogEvent(this, x.ToString(), LogType.Error);
                _response.Response = SetError("Database unavailable", 503);
            }
        }

        private void RequestGetStorage() {
            IList<WeaveBasicObject> wboList;
            string formatType = "json";

            if (_request.Id != null) {
                try {
                    wboList = _db.GetWboList(_request.UserId, _request.Collection, _request.Id, true);
                } catch (Exception x) {
                    RaiseLogEvent(this, x.ToString(), LogType.Error);
                    _response.Response = SetError("Database unavailable", 503);
                    return;
                }

                if (wboList.Count > 0) {
                    _response.Response = wboList[0].ToJson();
                } else {
                    _response.Response = SetError("record not found", 404);
                }
            } else {
                string full = QuerySegments["full"];
                string accept = Headers["accept"];
                if (!String.IsNullOrEmpty(accept)) {
                    if (accept.Contains("application/whoisi")) {
                        _response.Headers.Remove("Content-type");
                        _response.Headers.Add("Content-type", "application/whoisi");
                        formatType = "whoisi";
                    } else if (accept.Contains("application/newlines")) {
                        _response.Headers.Remove("Content-type");
                        _response.Headers.Add("Content-type", "application/newlines");
                        formatType = "newlines";
                    }
                }

                try {
                    wboList = _db.GetWboList(_request.UserId, _request.Collection, null, full == "1",
                                                    QuerySegments["newer"],
                                                    QuerySegments["older"],
                                                    QuerySegments["sort"],
                                                    QuerySegments["limit"],
                                                    QuerySegments["offset"],
                                                    QuerySegments["ids"],
                                                    QuerySegments["index_above"],
                                                    QuerySegments["index_below"]);

                    if (wboList.Count > 0) {
                        _response.Headers.Add("X-Weave-Records", wboList.Count + "");
                    }

                    var sb = new StringBuilder();
                    int commaFlag = 0;

                    switch (formatType) {
                        case "json":
                            sb.Append("[");
                            foreach (var wbo in wboList) {
                                if (commaFlag == 1) {
                                    sb.Append(",");
                                } else {
                                    commaFlag = 1;
                                }

                                if (full == "1") {
                                    sb.Append(wbo.ToJson());
                                } else {
                                    sb.Append(JsonConvert.SerializeObject(wbo.Id));
                                }
                            }
                            sb.Append("]");
                            break;
                        case "whoisi":
                            foreach (var wbo in wboList) {
                                string output;
                                if (full == "1") {
                                    output = wbo.ToJson();
                                } else {
                                    output = JsonConvert.SerializeObject(wbo.Id);
                                }

                                int length = Encoding.ASCII.GetByteCount(output);
                                byte[] byteArray = BitConverter.GetBytes(length);
                                if (BitConverter.IsLittleEndian) {
                                    Array.Reverse(byteArray);
                                }
                                sb.Append(byteArray).Append(output);
                            }
                            break;
                        case "newlines":
                            foreach (var wbo in wboList) {
                                if (full == "1") {
                                    sb.Append(wbo.ToJson().Replace("/\n/", "\u000a"));
                                } else {
                                    sb.Append(JsonConvert.SerializeObject(wbo.Id));
                                }

                                sb.Append("\n");
                            }
                            break;
                    }

                    _response.Response = sb.ToString();
                } catch (Exception x) {
                    RaiseLogEvent(this, x.ToString(), LogType.Error);
                    _response.Response = SetError("Database unavailable", 503);
                }
            }
        }

        private void RequestPut() {
            if (_request.HttpX != null && _db.GetMaxTimestamp(_request.UserId, _request.Collection) <= _request.HttpX.Value) {
                _response.Response = SetError(WeaveErrorCodes.NoOverwrite, 412);
                return;
            }

            var wbo = new WeaveBasicObject();

            if (!wbo.Populate(Body)) {
                _response.Response = SetError(WeaveErrorCodes.InvalidProtocol, 400);
                return;
            }

            if (wbo.Id == null && _request.Id != null) {
                wbo.Id = _request.Id;
            }

            wbo.Collection = CollectionDictionary.GetKey(_request.Collection);
            wbo.Modified = _request.RequestTime;

            if (wbo.Validate()) {
                try {
                    _db.SaveWbo(_request.UserId, wbo);
                } catch (Exception x) {
                    RaiseLogEvent(this, x.ToString(), LogType.Error);
                    _response.Response = SetError("Database unavailable", 503);
                    return;
                }
            } else {
                _response.Response = SetError(WeaveErrorCodes.InvalidWbo, 400);
                return;
            }

            if (wbo.Modified != null) {
                _response.Response = JsonConvert.SerializeObject(wbo.Modified.Value);
            }
        }

        private void RequestPost() {
            if (_request.Function == RequestFunction.Password) {
                try {
                    _db.ChangePassword(_request.UserId, Body);
                } catch (Exception x) {
                    RaiseLogEvent(this, x.ToString(), LogType.Error);
                    _response.Response = SetError("Database unavailable", 503);
                    return;
                }

                _response.Response = "success";
                return;
            }

            if (_request.HttpX != null && _db.GetMaxTimestamp(_request.UserId, _request.Collection) <= _request.HttpX.Value) {
                _response.Response = SetError(WeaveErrorCodes.NoOverwrite, 412);
                return;
            }

            var resultList = new ResultList(_request.RequestTime);
            var wboList = new Collection<WeaveBasicObject>();

            var dicArray = JsonConvert.DeserializeObject<Dictionary<string, object>[]>(Body);

            foreach (Dictionary<string, object> dic in dicArray) {
                var wbo = new WeaveBasicObject();

                if (!wbo.Populate(dic)) {
                    if (wbo.Id != null) {
                        resultList.FailedIds[wbo.Id] = new Collection<string> { "Failed to populate wbo." };
                    } else {
                        RaiseLogEvent(this, "Failed to populate wbo.", LogType.Error);
                    }
                    continue;
                }

                wbo.Collection = CollectionDictionary.GetKey(_request.Collection);
                wbo.Modified = _request.RequestTime;

                if (wbo.Validate()) {
                    wboList.Add(wbo);
                } else {
                    resultList.FailedIds[wbo.Id] = wbo.GetError();
                }
            }

            if (wboList.Count > 0) {
                try {
                    _db.SaveWboList(_request.UserId, wboList, resultList);
                } catch (Exception x) {
                    RaiseLogEvent(this, x.ToString(), LogType.Error);
                    _response.Response = SetError("Database unavailable", 503);
                    return;
                }
            }

            _response.Response = resultList.ToJson();
        }

        private void RequestDelete() {
            if (_request.HttpX != null && _db.GetMaxTimestamp(_request.UserId, _request.Collection) <= _request.HttpX.Value) {
                _response.Response = SetError(WeaveErrorCodes.NoOverwrite, 412);
                return;
            }

            if (_request.Id != null) {
                try {
                    _db.DeleteWbo(_request.UserId, _request.Collection, _request.Id);
                } catch (Exception x) {
                    RaiseLogEvent(this, x.ToString(), LogType.Error);
                    _response.Response = SetError("Database unavailable", 503);
                    return;
                }

                _response.Response = JsonConvert.SerializeObject(_request.RequestTime);
            } else if (_request.Collection != null) {
                try {
                    _db.DeleteWboList(_request.UserId, _request.Collection, null,
                                QuerySegments["newer"],
                                QuerySegments["older"],
                                QuerySegments["sort"],
                                QuerySegments["limit"],
                                QuerySegments["offset"],
                                QuerySegments["ids"],
                                QuerySegments["index_above"],
                                QuerySegments["index_below"]
                                );
                } catch (Exception x) {
                    RaiseLogEvent(this, x.ToString(), LogType.Error);
                    _response.Response = SetError("Database unavailable", 503);
                    return;
                }

                _response.Response = JsonConvert.SerializeObject(_request.RequestTime);
            } else {
                if (Headers["x-confirm-delete"] == null) {
                    SetError(WeaveErrorCodes.NoOverwrite, 412);
                }
            }
        }
        #endregion

        private string SetError(object message, int code) {
            switch (code) {
                case 400:
                    _response.ErrorStatus = "400 Bad Request";
                    break;
                case 401:
                    _response.ErrorStatus = "401 Unauthorized";
                    _response.Headers.Add("WWW-Authenticate", "Basic realm=\"Weave\"");
                    break;
                case 404:
                    _response.ErrorStatus = "404 Not Found";
                    break;
                case 412:
                    _response.ErrorStatus = "412 Precondition Failed";
                    break;
                case 503:
                    _response.ErrorStatus = "503 Service Unavailable";
                    break;
            }

            _response.ErrorStatusCode = code;

            return JsonConvert.SerializeObject(message);
        }
    }
}