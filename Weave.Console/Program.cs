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
using System.Configuration;
using NLog;
using NLog.Config;
using NLog.Targets;
using Nancy.Hosting.Self;
using Weave.Admin.Server;
using Weave.Server;

namespace Weave.Console {
    class Program {
		private static Logger _logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args) {
            string url = "https://";
            string adminUrl = "https://";
            bool enableAdmin = false;
            int adminPort = 0;

#if DEBUG
            var config = new LoggingConfiguration();
            var target = new ColoredConsoleTarget();
            target.RowHighlightingRules.Add(new ConsoleRowHighlightingRule { Condition = "level == LogLevel.Trace", ForegroundColor = ConsoleOutputColor.DarkGray });
            target.RowHighlightingRules.Add(new ConsoleRowHighlightingRule { Condition = "level == LogLevel.Info", ForegroundColor = ConsoleOutputColor.Green });
            target.RowHighlightingRules.Add(new ConsoleRowHighlightingRule { Condition = "level == LogLevel.Debug", ForegroundColor = ConsoleOutputColor.Yellow });
            target.RowHighlightingRules.Add(new ConsoleRowHighlightingRule { Condition = "level == LogLevel.Error", ForegroundColor = ConsoleOutputColor.Red });
            config.AddTarget("console", target);
            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, target));
            LogManager.Configuration = config;
#endif

            string enableAdminService = ConfigurationManager.AppSettings["EnableAdminService"];
            if (!String.IsNullOrEmpty(enableAdminService) && enableAdminService.ToLower() == "true") {
                enableAdmin = true;
            } else {
                _logger.Info("Admin webserver is disabled.");
            }

            int port;
            if (!Int32.TryParse(ConfigurationManager.AppSettings["Port"], out port)) {
                _logger.Error("Port missing in web.config. Exiting...");
                return;
            }

            if (enableAdmin) {
                if (!Int32.TryParse(ConfigurationManager.AppSettings["AdminPort"], out adminPort) || adminPort == 0) {
                    _logger.Error("AdminPort missing or incorrect in web.config. Exiting...");
                    return;
                } 

                if (adminPort == port) {
                    _logger.Error("AdminPort and Port cannot be the same. Exiting...");
                    return;
                }

                string enableAdminSsl = ConfigurationManager.AppSettings["EnableAdminSsl"];
                if (!String.IsNullOrEmpty(enableAdminSsl) && enableAdminSsl.ToLower() == "false") {
                    adminUrl = "http://";
                }
            }

            string enableSsl = ConfigurationManager.AppSettings["EnableSsl"];
            if (!String.IsNullOrEmpty(enableSsl) && enableSsl.ToLower() == "false") {
                url = "http://";
            }

#if DEBUG
            url = "http://";
            adminUrl = "http://";
#endif

            url += "localhost:" + port;
            var nancyHost = new NancyHost(new Uri(url), new BootStrapper());
            nancyHost.Start();

            _logger.Info("Webserver started at " + url);

            NancyHost nancyAdminHost = null;
            if (enableAdmin && adminPort > 0) {
                adminUrl += "localhost:" + adminPort;
                nancyAdminHost = new NancyHost(new Uri(adminUrl), new AdminBootStrapper());
                nancyAdminHost.Start();

                _logger.Info("Admin webserver started at " + adminUrl);
            }

            System.Console.WriteLine("Press any key to exit...");
            System.Console.ReadLine();

            nancyHost.Stop();
            if (nancyAdminHost != null) {
                nancyAdminHost.Stop();
            }
        }
    }
}
