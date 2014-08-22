using System;
using System.Configuration;
using Nancy.Hosting.Self;
using NLog;
using Weave.Core.Models;

namespace Weave.Server {
	public class WeaveHost {
		private NancyHost _nancyHost;
		private static Logger _logger = LogManager.GetCurrentClassLogger();
		WeaveConfigurationSection _config = (WeaveConfigurationSection)ConfigurationManager.GetSection("weave");

		public void Start() {
			if (_config == null || _nancyHost != null) {
				return;
			}
#if(DEBUG)
			string url = "http://";
#else
			string url = "https://";
			if (!_config.EnableSsl) {
				url = "http://";
			}
#endif
			url += "localhost:" + _config.Port;
			_nancyHost = new NancyHost(new Uri(url), new BootStrapper());
			_nancyHost.Start();

			_logger.Info("Weave server started at " + url);
		}

		public void Stop() {
			if (_nancyHost != null) {
				_nancyHost.Stop();
				_nancyHost = null;

				_logger.Info("Weave server stopped");
			}
		}
	}
}