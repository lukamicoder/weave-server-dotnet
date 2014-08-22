using System;
using System.Configuration;
using Nancy.Hosting.Self;
using NLog;
using Weave.Core.Models;

namespace Weave.Server.Admin {
	public class WeaveAdminHost {
		private NancyHost _nancyHost;
		private static Logger _logger = LogManager.GetCurrentClassLogger();
		WeaveConfigurationSection _config = (WeaveConfigurationSection)ConfigurationManager.GetSection("weave");

		public void Start() {
			if (_config == null || _nancyHost != null) {
				return;
			}


			if (!_config.EnableAdminService) {
				_logger.Info("Weave admin server is disabled.");
			} else {
				if (_config.AdminPort == _config.Port) {
					_logger.Error("AdminPort and Port cannot be the same. Exiting...");
					return;
				}

#if(DEBUG)
				string url = "http://";
#else
				string url = "https://";
				if (!_config.EnableAdminSsl) {
					url = "http://";
				}
#endif

				url += "localhost:" + _config.AdminPort;
				_nancyHost = new NancyHost(new Uri(url), new AdminBootStrapper());
				_nancyHost.Start();

				_logger.Info("Weave admin server started at " + url);
			}
		}

		public void Stop() {
			if (_nancyHost != null) {
				_nancyHost.Stop();
				_nancyHost = null;

				_logger.Info("Weave admin server stopped");
			}
		}
	}
}