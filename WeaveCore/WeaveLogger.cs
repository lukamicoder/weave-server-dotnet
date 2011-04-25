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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace WeaveCore {
    enum LogType {
        Error = 0,
        Information = 1,
        Warning = 2,
        Debug = 3,
    }

    static class WeaveLogger {
        [DllImport("advapi32.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern bool LogonUser(string lpszUsername, string lpszDomain, string lpszPassword, int dwLogonType, int dwLogonProvider, ref IntPtr phToken);

        public static void WriteMessage(string msg, LogType type) {
            const string appName = "Weave Server";
            if (!EventLog.SourceExists(appName)) {
                EventLog.CreateEventSource(appName, "Application");
            }

            IntPtr token = IntPtr.Zero;
            LogonUser(ConfigurationManager.AppSettings["LoggingUser.UserName"],
                      ConfigurationManager.AppSettings["LoggingUser.Domain"],
                      ConfigurationManager.AppSettings["LoggingUser.Password"], 2, 0, ref token);

            if (token == IntPtr.Zero) {
                return;
            }

            WindowsIdentity identity = new WindowsIdentity(token);
            WindowsImpersonationContext impersonationContext = null;

            try {
                impersonationContext = identity.Impersonate();

                using (EventLog eventLog = new EventLog()) {
                    eventLog.Source = appName;
                    switch (type) {
                        case LogType.Error:
                            StackTrace stackTrace = new StackTrace();
                            StackFrame stackFrame = stackTrace.GetFrame(1);
                            MethodBase methodBase = stackFrame.GetMethod();
                            msg = "(" + methodBase.ReflectedType.Name + "." + methodBase.Name + ") " + msg;
                            eventLog.WriteEntry(msg, EventLogEntryType.Error);
                            break;
                        case LogType.Warning:
                            eventLog.WriteEntry(msg, EventLogEntryType.Warning);
                            break;
                        case LogType.Information:
                            eventLog.WriteEntry(msg, EventLogEntryType.Information);
                            break;
                    }
                }
            } finally {
                if (impersonationContext != null) {
                    impersonationContext.Undo();
                }
            }
        }
    }
}
