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
using System.ServiceProcess;
using NLog;
using NLog.Config;
using NLog.Targets;
using Weave.Server;
using Weave.Server.Admin;

namespace Weave.Service {
	public class Program : ServiceBase {
		WeaveHost _host = new WeaveHost();
		WeaveAdminHost _adminHost = new WeaveAdminHost();
		private static Logger _logger = LogManager.GetCurrentClassLogger();

		static void Main() {
#if(DEBUG)
			var config = new LoggingConfiguration();
			var target = new ColoredConsoleTarget();
			target.RowHighlightingRules.Add(new ConsoleRowHighlightingRule { Condition = "level == LogLevel.Trace", ForegroundColor = ConsoleOutputColor.DarkGray });
			target.RowHighlightingRules.Add(new ConsoleRowHighlightingRule { Condition = "level == LogLevel.Info", ForegroundColor = ConsoleOutputColor.Green });
			target.RowHighlightingRules.Add(new ConsoleRowHighlightingRule { Condition = "level == LogLevel.Debug", ForegroundColor = ConsoleOutputColor.Yellow });
			target.RowHighlightingRules.Add(new ConsoleRowHighlightingRule { Condition = "level == LogLevel.Error", ForegroundColor = ConsoleOutputColor.Red });
			config.AddTarget("console", target);
			config.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, target));
			LogManager.Configuration = config;

			var service = new Program();
			service.OnStart(null);

			Console.WriteLine("Press any key to exit...");

			Console.ReadKey();
			service.OnStop();
#else
			ServiceBase.Run(new Program());
#endif
		}

		protected override void OnStart(string[] args) {
			_host.Start();
			_adminHost.Start();
			_logger.Info("Weave service started.");
		}

		protected override void OnStop() {
			_host.Stop();
			_adminHost.Stop();

			_logger.Info("Weave service stopped.");
		}
	}
}
