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
using System.Configuration;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace WeaveCleanup {
    class Program {
        static void Main(string[] args) {
            string appName = "Weave Cleanup";
            string host = ConfigurationManager.AppSettings["WeaveServer.URL"];

            if (!EventLog.SourceExists(appName)) {
                EventLog.CreateEventSource(appName, "Application");
            }

            using (EventLog eventLog = new EventLog()) {
                eventLog.Source = appName;

                if (!String.IsNullOrEmpty(host)) {
                    string url = String.Format("{0}/Cleanup/", host);
                    //only needed if the connection is made through SSL and the certification is not valid.
                    ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

                    var client = new WebClient();
                    try {
                        byte[] data = client.DownloadData(new Uri(url));
                        string response = Encoding.UTF8.GetString(data);
                        int no;
                        if (Int32.TryParse(response, out no)) {
                            string msg;
                            switch (no) {
                                case 0:
                                    msg = "No record has been deleted.";
                                    break;
                                case 1:
                                    msg = "One record has been deleted.";
                                    break;
                                default:
                                    msg = String.Format("{0} records have been deleted.", no);
                                    break;
                            }
                            eventLog.WriteEntry(msg, EventLogEntryType.Information);
                        } else {
                            eventLog.WriteEntry(response, EventLogEntryType.Error);
                        }
                    } catch (Exception ex) {
                        eventLog.WriteEntry(ex.Message, EventLogEntryType.Error);
                    }
                } else {
                    eventLog.WriteEntry("Incorrect configuration.", EventLogEntryType.Error);
                }
            }
        }
    }
}
