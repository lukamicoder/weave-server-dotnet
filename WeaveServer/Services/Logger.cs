﻿/* 
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
using System.Configuration;
using System.Diagnostics;
using System.Security;
using WeaveCore;

namespace WeaveServer.Services {
    static class Logger {
        public static void WriteMessage(string msg, LogType type) {
#if DEBUG
            Debug.WriteLine(msg);
            return;
#endif
            string sourceName = ConfigurationManager.AppSettings["EventLogSourceName"];

            if (String.IsNullOrEmpty(sourceName)) {
                return;
            }

            try {
                if (!EventLog.SourceExists(sourceName)) {
                    return;
                }
            } catch (SecurityException) {
                return;
            }

            using (EventLog eventLog = new EventLog()) {
                eventLog.Source = sourceName;
                switch (type) {
                    case LogType.Error:
                        eventLog.WriteEntry(msg, EventLogEntryType.Error, 10);
                        break;
                    case LogType.Warning:
                        eventLog.WriteEntry(msg, EventLogEntryType.Warning, 20);
                        break;
                    case LogType.Information:
                        eventLog.WriteEntry(msg, EventLogEntryType.Information, 30);
                        break;
                }
            }
        }
    }
}
