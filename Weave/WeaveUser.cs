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
using System.Text;
using System.Web.Script.Serialization;

namespace Weave {
    public class WeaveUser {
        JavaScriptSerializer jss;
        WeaveStorageUser db;

        public WeaveRequest Request { get; private set; }
        public Dictionary<string, string> Headers { get; private set; }
        public string ErrorStatus { get; private set; }
        public int ErrorStatusCode { get; private set; }
        public string Response { get; private set; }

        double? serverTime;
        public double ServerTime {
            get {
                if (serverTime == null) {
                    TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);
                    serverTime = Math.Round(ts.TotalSeconds, 2);
                }

                return serverTime.Value;
            }
        }

        public WeaveUser(WeaveRequest request) {
            Request = request;

            jss = new JavaScriptSerializer();

            Headers = new Dictionary<string, string>();
            Headers.Add("Content-type", "application/json");
            Headers.Add("X-Weave-Timestamp", ServerTime + "");

            if (!Request.IsValid) {
                if (Request.ErrorMessage != 0) {
                    Response = ReportProblem(Request.ErrorMessage, Request.ErrorCode);
                }

                return;
            }

            try {
                db = new WeaveStorageUser(Request.UserName);

                if (!db.AuthenticateUser(Request.Password)) {
                    Response = ReportProblem("Authentication failed", 401);
                    return;
                }
            } catch (WeaveException x) {
                Response = ReportProblem(x.Message, x.Code);
                return;
            }

            switch (Request.RequestMethod) {
                case "GET":
                    switch (Request.Function) {
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
            switch (Request.Collection) {
                case "quota":
                    Response = jss.Serialize(new[] { db.GetStorageTotal() });
                    break;
                case "collections":
                    Response = jss.Serialize(db.GetCollectionListWithTimestamps());
                    break;
                case "collection_counts":
                    Response = jss.Serialize(db.GetCollectionListWithCounts());
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

            if (Request.Id != null) {
                try {
                    wboList = db.RetrieveWboList(Request.Collection, Request.Id, true, null, null, null, null, null, null, null, null, null, null);
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
                full = Request.QueryString["full"];
                string accept = Request.ServerVariables["HTTP_ACCEPT"];
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
                    wboList = db.RetrieveWboList(Request.Collection, null, full == "1" ? true : false,
                                                    Request.QueryString["parentid"],
                                                    Request.QueryString["predecessorid"],
                                                    Request.QueryString["newer"],
                                                    Request.QueryString["older"],
                                                    Request.QueryString["sort"],
                                                    Request.QueryString["limit"],
                                                    Request.QueryString["offset"],
                                                    Request.QueryString["ids"],
                                                    Request.QueryString["index_above"],
                                                    Request.QueryString["index_below"]);

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
                                    sb.Append(jss.Serialize(wbo.Id));
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
                                    output = jss.Serialize(wbo.Id);
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
                                    sb.Append(jss.Serialize(wbo.Id));
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
            if (Request.HttpX != null && db.GetMaxTimestamp(Request.Collection) <= Request.HttpX.Value) {
                Response = ReportProblem(WeaveErrorCodes.NoOverwrite, 412);
                return;
            }

            WeaveBasicObject wbo = new WeaveBasicObject();

            if (!wbo.Populate(Request.Content)) {
                Response = ReportProblem(WeaveErrorCodes.InvalidProtocol, 400);
                return;
            }

            if (wbo.Id == null && Request.Id != null) {
                wbo.Id = Request.Id;
            }

            wbo.Collection = Request.Collection;
            wbo.Modified = ServerTime;

            if (wbo.Validate()) {
                try {
                    db.StoreOrUpdateWbo(wbo);
                } catch (WeaveException e) {
                    Response = ReportProblem(e.Message, e.Code);
                    return;
                }
            } else {
                Response = ReportProblem(WeaveErrorCodes.InvalidWbo, 400);
                return;
            }

            if (wbo.Modified != null) {
                 Response = jss.Serialize(wbo.Modified.Value);
            }
        }

        private void RequestPost() {
            if (Request.Function == "password") {
                try {
                    db.ChangePassword(Request.Content);
                } catch (WeaveException e) {
                    Response = ReportProblem(e.Message, e.Code);
                    return;
                }

                 Response = "success";
                return;
            }

            if (Request.HttpX != null && db.GetMaxTimestamp(Request.Collection) <= Request.HttpX.Value) {
                Response = ReportProblem(WeaveErrorCodes.NoOverwrite, 412);
                return;
            }

            WeaveBasicObject wbo;
            var resultList = new WeaveResultList();
            var wboList = new Collection<WeaveBasicObject>();

            object obj = jss.DeserializeObject(Request.Content);
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

                wbo.Collection = Request.Collection;
                wbo.Modified = ServerTime;

                if (wbo.Validate()) {
                    wboList.Add(wbo);
                } else {
                    resultList.FailedIds[wbo.Id] = wbo.GetError();
                    wbo.ClearError();
                }
            }

            if (wboList.Count > 0) {
                try {
                    db.StoreOrUpdateWboList(wboList, resultList);
                } catch (WeaveException e) {
                    Response = ReportProblem(e.Message, e.Code);
                    return;
                }
            }

             Response = resultList.ToJson();
        }

        private void RequestDelete() {
            if (Request.HttpX != null && db.GetMaxTimestamp(Request.Collection) <= Request.HttpX.Value) {
                ReportProblem(WeaveErrorCodes.NoOverwrite, 412);
                return;
            }

            if (Request.Id != null) {
                try {
                    db.DeleteWbo(Request.Collection, Request.Id);
                } catch (WeaveException e) {
                    Response = ReportProblem(e.Message, e.Code);
                    return;
                }

                 Response = jss.Serialize(ServerTime);
            } else {
                try {
                    db.DeleteWboList(Request.Collection, null,
                                Request.QueryString["parentid"],
                                Request.QueryString["predecessorid"],
                                Request.QueryString["newer"],
                                Request.QueryString["older"],
                                Request.QueryString["sort"],
                                Request.QueryString["limit"],
                                Request.QueryString["offset"],
                                Request.QueryString["ids"],
                                Request.QueryString["index_above"],
                                Request.QueryString["index_below"]
                                );
                } catch (WeaveException e) {
                    Response = ReportProblem(e.Message, e.Code);
                    return;
                }

                 Response = jss.Serialize(ServerTime);
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

            return jss.Serialize(message);
        }
    }
}