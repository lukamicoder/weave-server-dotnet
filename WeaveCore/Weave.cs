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
using System.IO;
using System.Text;
using Newtonsoft.Json;
using WeaveCore.Models;
using WeaveCore.Repository;

namespace WeaveCore {
    public class Weave : WeaveLogEventBase {
        readonly DBRepository _db;
        readonly WeaveRequest _req;

        public Dictionary<string, string> Headers { get; private set; }
        public string ErrorStatus { get; private set; }
        public int ErrorStatusCode { get; private set; }
        public string Response { get; private set; }

        public Weave(Uri url, NameValueCollection serverVariables, NameValueCollection queryString, Stream inputStream) {
            _req = new WeaveRequest(url, serverVariables, queryString, inputStream);

            Headers = new Dictionary<string, string> { { "Content-type", "application/json" }, { "X-Weave-Timestamp", _req.RequestTime + "" } };

            if (!_req.IsValid) {
                if (_req.ErrorMessage != 0) {
                    Response = ReportProblem(_req.ErrorMessage, _req.ErrorCode);
                }

                return;
            }

            if (_req.PathName == "user") {
                if (!RequestUser()) {
                    return;
                }
            }

            try {
                _db = new DBRepository();

                if (_db.AuthenticateUser(_req.UserName, _req.Password) == 0) {
                    Response = ReportProblem("Authentication failed", 401);
                    return;
                }
            } catch (Exception x) {
                RaiseLogEvent(this, x.ToString(), LogType.Error);
                Response = ReportProblem("Database unavailable", 503);
                return;
            }

            switch (_req.RequestMethod) {
                case RequestMethod.GET:
                    switch (_req.Function) {
                        case RequestFunction.Info:
                            RequestGetInfo();
                            break;
                        case RequestFunction.Storage:
                            RequestGetStorage();
                            break;
                        case RequestFunction.Node:
                            Response = _req.Url;
                            break;
                        default:
                            Response = ReportProblem(WeaveErrorCodes.InvalidProtocol, 400);
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
                    Response = ReportProblem(WeaveErrorCodes.InvalidProtocol, 400);
                    break;
            }
        }

        private void RequestGetInfo() {
            try {
                switch (_req.Collection) {
                    case "quota":
                        Response = JsonConvert.SerializeObject(new[] { _db.GetStorageTotal() });
                        break;
                    case "collections":
                        Response = JsonConvert.SerializeObject(_db.GetCollectionListWithTimestamps());
                        break;
                    case "collection_counts":
                        Response = JsonConvert.SerializeObject(_db.GetCollectionListWithCounts());
                        break;
                    case "collection_usage":
                        Response = JsonConvert.SerializeObject(_db.GetCollectionStorageTotals());
                        break;
                    default:
                        Response = ReportProblem(WeaveErrorCodes.InvalidProtocol, 400);
                        break;
                }
            } catch (Exception x) {
                RaiseLogEvent(this, x.ToString(), LogType.Error);
                Response = ReportProblem("Database unavailable", 503);
            }
        }

        private void RequestGetStorage() {
            IList<WeaveBasicObject> wboList;
            string formatType = "json";

            if (_req.Id != null) {
                try {
                    wboList = _db.GetWboList(_req.Collection, _req.Id, true);
                } catch (Exception x) {
                    RaiseLogEvent(this, x.ToString(), LogType.Error);
                    Response = ReportProblem("Database unavailable", 503);
                    return;
                }

                if (wboList.Count > 0) {
                    Response = wboList[0].ToJson();
                } else {
                    Response = ReportProblem("record not found", 404);
                }
            } else {
                string full = _req.QueryString["full"];
                string accept = _req.ServerVariables["HTTP_ACCEPT"];
                if (accept != null) {
                    if (accept.Contains("application/whoisi")) {
                        Headers.Remove("Content-type");
                        Headers.Add("Content-type", "application/whoisi");
                        formatType = "whoisi";
                    } else if (accept.Contains("application/newlines")) {
                        Headers.Remove("Content-type");
                        Headers.Add("Content-type", "application/newlines");
                        formatType = "newlines";
                    }
                }

                try {
                    wboList = _db.GetWboList(_req.Collection, null, full == "1",
                                                    _req.QueryString["newer"],
                                                    _req.QueryString["older"],
                                                    _req.QueryString["sort"],
                                                    _req.QueryString["limit"],
                                                    _req.QueryString["offset"],
                                                    _req.QueryString["ids"],
                                                    _req.QueryString["index_above"],
                                                    _req.QueryString["index_below"]);

                    if (wboList.Count > 0) {
                        Headers.Add("X-Weave-Records", wboList.Count + "");
                    }

                    StringBuilder sb = new StringBuilder();
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

                    Response = sb.ToString();
                } catch (Exception x) {
                    RaiseLogEvent(this, x.ToString(), LogType.Error);
                    Response = ReportProblem("Database unavailable", 503);
                }
            }
        }

        private void RequestPut() {
            if (_req.HttpX != null && _db.GetMaxTimestamp(_req.Collection) <= _req.HttpX.Value) {
                Response = ReportProblem(WeaveErrorCodes.NoOverwrite, 412);
                return;
            }

            var wbo = new WeaveBasicObject();

            if (!wbo.Populate(_req.Content)) {
                Response = ReportProblem(WeaveErrorCodes.InvalidProtocol, 400);
                return;
            }

            if (wbo.Id == null && _req.Id != null) {
                wbo.Id = _req.Id;
            }

            wbo.Collection = WeaveCollectionDictionary.GetKey(_req.Collection);
            wbo.Modified = _req.RequestTime;

            if (wbo.Validate()) {
                try {
                    _db.SaveWbo(wbo);
                } catch (Exception x) {
                    RaiseLogEvent(this, x.ToString(), LogType.Error);
                    Response = ReportProblem("Database unavailable", 503);
                    return;
                }
            } else {
                Response = ReportProblem(WeaveErrorCodes.InvalidWbo, 400);
                return;
            }

            if (wbo.Modified != null) {
                Response = JsonConvert.SerializeObject(wbo.Modified.Value);
            }
        }

        private void RequestPost() {
            if (_req.Function == RequestFunction.Password) {
                try {
                    _db.ChangePassword(_req.Content);
                } catch (Exception x) {
                    RaiseLogEvent(this, x.ToString(), LogType.Error);
                    Response = ReportProblem("Database unavailable", 503);
                    return;
                }

                Response = "success";
                return;
            }

            if (_req.HttpX != null && _db.GetMaxTimestamp(_req.Collection) <= _req.HttpX.Value) {
                Response = ReportProblem(WeaveErrorCodes.NoOverwrite, 412);
                return;
            }

            var resultList = new WeaveResultList(_req.RequestTime);
            var wboList = new Collection<WeaveBasicObject>();

            var dicArray = JsonConvert.DeserializeObject<Dictionary<string, object>[]>(_req.Content);

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

                wbo.Collection = WeaveCollectionDictionary.GetKey(_req.Collection);
                wbo.Modified = _req.RequestTime;

                if (wbo.Validate()) {
                    wboList.Add(wbo);
                } else {
                    resultList.FailedIds[wbo.Id] = wbo.GetError();
                }
            }

            if (wboList.Count > 0) {
                try {
                    _db.SaveWboList(wboList, resultList);
                } catch (Exception x) {
                    RaiseLogEvent(this, x.ToString(), LogType.Error);
                    Response = ReportProblem("Database unavailable", 503);
                    return;
                }
            }

            Response = resultList.ToJson();
        }

        private void RequestDelete() {
            if (_req.HttpX != null && _db.GetMaxTimestamp(_req.Collection) <= _req.HttpX.Value) {
                Response = ReportProblem(WeaveErrorCodes.NoOverwrite, 412);
                return;
            }

            if (_req.Id != null) {
                try {
                    _db.DeleteWbo(_req.Collection, _req.Id);
                } catch (Exception x) {
                    RaiseLogEvent(this, x.ToString(), LogType.Error);
                    Response = ReportProblem("Database unavailable", 503);
                    return;
                }

                Response = JsonConvert.SerializeObject(_req.RequestTime);
            } else if (_req.Collection != null) {
                try {
                    _db.DeleteWboList(_req.Collection, null,
                                _req.QueryString["newer"],
                                _req.QueryString["older"],
                                _req.QueryString["sort"],
                                _req.QueryString["limit"],
                                _req.QueryString["offset"],
                                _req.QueryString["ids"],
                                _req.QueryString["index_above"],
                                _req.QueryString["index_below"]
                                );
                } catch (Exception x) {
                    RaiseLogEvent(this, x.ToString(), LogType.Error);
                    Response = ReportProblem("Database unavailable", 503);
                    return;
                }

                Response = JsonConvert.SerializeObject(_req.RequestTime);
            } else {
                if (_req.ServerVariables["HTTP_X_CONFIRM_DELETE"] == null) {
                    ReportProblem(WeaveErrorCodes.NoOverwrite, 412);
                }
            }
        }

        private bool RequestUser() {
            if (_req.UserName == "a") {
                Response = "0";
                return false;
            }

            if (_req.UserName.Length == 32) {
                var wa = new WeaveAdmin();
                if (_req.RequestMethod == RequestMethod.GET && _req.Function == RequestFunction.NotSupported) {
                    Response = wa.IsUserNameUnique(_req.UserName) ? "0" : "1";
                    return false;
                }

                if (_req.RequestMethod == RequestMethod.PUT) {
                    var dic = JsonConvert.DeserializeObject<Dictionary<string, object>>(_req.Content);

                    if (dic == null || dic.Count == 0) {
                        Response = ReportProblem("Unable to extract from json", 400);
                        return false;
                    }

                    string output = wa.CreateUser(_req.UserName, (string)dic["password"], (string)dic["email"]);
                    Response = String.IsNullOrEmpty(output) ? _req.UserName : output;
                    return false;
                }
            }

            return true;
        }

        private string ReportProblem(object message, int code) {
            switch (code) {
                case 400:
                    ErrorStatus = "400 Bad Request";
                    break;
                case 401:
                    ErrorStatus = "401 Unauthorized";
                    Headers.Add("WWW-Authenticate", "Basic realm=\"Weave\"");
                    break;
                case 404:
                    ErrorStatus = "404 Not Found";
                    break;
                case 412:
                    ErrorStatus = "412 Precondition Failed";
                    break;
                case 503:
                    ErrorStatus = "503 Service Unavailable";
                    break;
            }

            ErrorStatusCode = code;

            return JsonConvert.SerializeObject(message);
        }
    }
}