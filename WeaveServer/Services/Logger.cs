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
using System.Configuration;
using System.Diagnostics;
using System.Security;
using WeaveCore;

namespace WeaveServer.Services {
    static class Logger {
        public static void WriteMessage(string msg, LogType type) {
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
