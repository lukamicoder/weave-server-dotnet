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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace Weave {
    public class WeaveUser {
        JavaScriptSerializer _jss;
        WeaveStorageUser _db;
        WeaveRequest _req;

        public Dictionary<string, string> Headers { get; private set; }
        public string ErrorStatus { get; private set; }
        public int ErrorStatusCode { get; private set; }
        public string Response { get; private set; }

        public WeaveUser(NameValueCollection serverVariables, NameValueCollection queryString, string rawUrl, Stream inputStream) {
            _req = new WeaveRequest(serverVariables, queryString, rawUrl, inputStream);

            _jss = new JavaScriptSerializer();

            Headers = new Dictionary<string, string>();
            Headers.Add("Content-type", "application/json");
            Headers.Add("X-Weave-Timestamp", _req.RequestTime + "");

            if (!_req.IsValid) {
                if (_req.ErrorMessage != 0) {
                    Response = ReportProblem(_req.ErrorMessage, _req.ErrorCode);
                }

                return;
            }

            try {
                _db = new WeaveStorageUser();

                if (!_db.AuthenticateUser(_req.UserName, _req.Password)) {
                    Response = ReportProblem("Authentication failed", 401);
                    return;
                }
            } catch (WeaveException x) {
                Response = ReportProblem(x.Message, x.Code);
                return;
            }

            switch (_req.RequestMethod) {
                case "GET":
                    switch (_req.Function) {
                        case "info":
                            RequestGetInfo();
                            break;
                        case "storage":
                            RequestGetStorage();
                            break;
                        default:
                            Response = ReportProblem(WeaveErrorCodes.InvalidProtocol, 400);
                            break;
                    }
                    break;
                case "PUT":
                    RequestPut();
                    break;
                case "POST":
                    RequestPost();
                    break;
                case "DELETE":
                    RequestDelete();
                    break;
                default:
                    Response = ReportProblem(WeaveErrorCodes.InvalidProtocol, 400);
                    break;
            }
        }

        private void RequestGetInfo() {
            switch (_req.Collection) {
                case "quota":
                    Response = _jss.Serialize(new[] { _db.GetStorageTotal() });
                    break;
                case "collections":
                    Response = _jss.Serialize(_db.GetCollectionListWithTimestamps());
                    break;
                case "collection_counts":
                    Response = _jss.Serialize(_db.GetCollectionListWithCounts());
                    break;
                case "collection_usage":
                    Response = _jss.Serialize(_db.GetCollectionStorageTotals());
                    break;
                default:
                    Response = ReportProblem(WeaveErrorCodes.InvalidProtocol, 400);
                    break;
            }
        }

        private void RequestGetStorage() {
            IList<WeaveBasicObject> wboList;
            string full;
            string formatType = "json";

            if (_req.Id != null) {
                try {
                    wboList = _db.RetrieveWboList(_req.Collection, _req.Id, true, null, null, null, null, null, null, null, null, null, null);
                } catch (WeaveException e) {
                    Response = ReportProblem(e.Message, e.Code);
                    return;
                }

                if (wboList.Count > 0) {
                    Response = wboList[0].ToJson();
                } else {
                    Response = ReportProblem("record not found", 404);
                    return;
                }
            } else {
                full = _req.QueryString["full"];
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
                    wboList = _db.RetrieveWboList(_req.Collection, null, full == "1" ? true : false,
                                                    _req.QueryString["parentid"],
                                                    _req.QueryString["predecessorid"],
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
                            foreach (WeaveBasicObject wbo in wboList) {
                                if (commaFlag == 1) {
                                    sb.Append(",");
                                } else {
                                    commaFlag = 1;
                                }

                                if (full == "1") {
                                    sb.Append(wbo.ToJson());
                                } else {
                                    sb.Append(_jss.Serialize(wbo.Id));
                                }
                            }
                            sb.Append("]");
                            break;
                        case "whoisi":
                            foreach (WeaveBasicObject wbo in wboList) {
                                string output;
                                if (full == "1") {
                                    output = wbo.ToJson();
                                } else {
                                    output = _jss.Serialize(wbo.Id);
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
                            foreach (WeaveBasicObject wbo in wboList) {
                                if (full == "1") {
                                    sb.Append(wbo.ToJson().Replace("/\n/", "\u000a"));
                                } else {
                                    sb.Append(_jss.Serialize(wbo.Id));
                                }

                                sb.Append("\n");
                            }
                            break;
                    }

                    Response = sb.ToString();
                } catch (WeaveException e) {
                    Response = ReportProblem(e.Message, e.Code);
                }
            }
        }

        private void RequestPut() {
            if (_req.HttpX != null && _db.GetMaxTimestamp(_req.Collection) <= _req.HttpX.Value) {
                Response = ReportProblem(WeaveErrorCodes.NoOverwrite, 412);
                return;
            }

            WeaveBasicObject wbo = new WeaveBasicObject();

            if (!wbo.Populate(_req.Content)) {
                Response = ReportProblem(WeaveErrorCodes.InvalidProtocol, 400);
                return;
            }

            if (wbo.Id == null && _req.Id != null) {
                wbo.Id = _req.Id;
            }

            wbo.Collection = _req.Collection;
            wbo.Modified = _req.RequestTime;

            if (wbo.Validate()) {
                try {
                    _db.StoreOrUpdateWbo(wbo);
                } catch (WeaveException e) {
                    Response = ReportProblem(e.Message, e.Code);
                    return;
                }
            } else {
                Response = ReportProblem(WeaveErrorCodes.InvalidWbo, 400);
                return;
            }

            if (wbo.Modified != null) {
                Response = _jss.Serialize(wbo.Modified.Value);
            }
        }

        private void RequestPost() {
            if (_req.Function == "password") {
                try {
                    _db.ChangePassword(_req.Content);
                } catch (WeaveException e) {
                    Response = ReportProblem(e.Message, e.Code);
                    return;
                }

                Response = "success";
                return;
            }

            if (_req.HttpX != null && _db.GetMaxTimestamp(_req.Collection) <= _req.HttpX.Value) {
                Response = ReportProblem(WeaveErrorCodes.NoOverwrite, 412);
                return;
            }

            WeaveBasicObject wbo;
            var resultList = new WeaveResultList(_req.RequestTime);
            var wboList = new Collection<WeaveBasicObject>();

            object obj = _jss.DeserializeObject(_req.Content);
            object[] objArray;

            if (obj != null) {
                try {
                    objArray = (object[])obj;
                } catch (InvalidCastException) {
                    Response = ReportProblem(WeaveErrorCodes.JsonParse, 400);
                    return;
                }
            } else {
                Response = ReportProblem(WeaveErrorCodes.JsonParse, 400);
                return;
            }

            foreach (object objItem in objArray) {
                wbo = new WeaveBasicObject();
                Dictionary<string, object> dic;

                try {
                    dic = (Dictionary<string, object>)objItem;
                } catch (InvalidCastException) {
                    WeaveLogger.WriteMessage("Failed to extract wbo from POST content.", LogType.Error);
                    continue;
                }

                if (!wbo.Populate(dic)) {
                    if (wbo.Id != null) {
                        resultList.FailedIds[wbo.Id] = new Collection<string> { "Failed to populate wbo." };
                    } else {
                        WeaveLogger.WriteMessage("Failed to populate wbo.", LogType.Error);
                    }
                    continue;
                }

                wbo.Collection = _req.Collection;
                wbo.Modified = _req.RequestTime;

                if (wbo.Validate()) {
                    wboList.Add(wbo);
                } else {
                    resultList.FailedIds[wbo.Id] = wbo.GetError();
                    wbo.ClearError();
                }
            }

            if (wboList.Count > 0) {
                try {
                    _db.StoreOrUpdateWboList(wboList, resultList);
                } catch (WeaveException e) {
                    Response = ReportProblem(e.Message, e.Code);
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
                } catch (WeaveException e) {
                    Response = ReportProblem(e.Message, e.Code);
                    return;
                }

                Response = _jss.Serialize(_req.RequestTime);
            } else if (_req.Collection != null) {
                try {
                    _db.DeleteWboList(_req.Collection, null,
                                _req.QueryString["parentid"],
                                _req.QueryString["predecessorid"],
                                _req.QueryString["newer"],
                                _req.QueryString["older"],
                                _req.QueryString["sort"],
                                _req.QueryString["limit"],
                                _req.QueryString["offset"],
                                _req.QueryString["ids"],
                                _req.QueryString["index_above"],
                                _req.QueryString["index_below"]
                                );
                } catch (WeaveException e) {
                    Response = ReportProblem(e.Message, e.Code);
                    return;
                }

                Response = _jss.Serialize(_req.RequestTime);
            } else {
                if (_req.ServerVariables["HTTP_X_CONFIRM_DELETE"] == null) {
                    ReportProblem(WeaveErrorCodes.NoOverwrite, 412);
                    return;
                }

                _db.DeleteUser(_db.UserId);
            }
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

            return _jss.Serialize(message);
        }
    }
}