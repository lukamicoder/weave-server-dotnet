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
using System.ServiceProcess;
using NLog;
using Nancy.Hosting.Self;
using Weave.Admin.Server;
using Weave.Server;

//netsh http add sslcert ipport=0.0.0.0:8888 certhash=123311cf507eecf2b91eb6ce6789be60b0c7819b appid={6f911c69-f3d5-4697-8600-fcf342a2c676}

namespace WeaveService {
    public partial class Service : ServiceBase {
        private NancyHost _nancyHost;
        private NancyHost _nancyAdminHost;
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public Service() {
            InitializeComponent();
        }

        protected override void OnStart(string[] args) {
            string url = "https://";
            string adminUrl = "https://";
            bool enableAdmin = false;
            int adminPort = 0;

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
                if (!Int32.TryParse(ConfigurationManager.AppSettings["AdminPort"], out adminPort)) {
                    _logger.Error("AdminPort missing in web.config. Exiting...");
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

            url += "localhost:" + port;
            _nancyHost = new NancyHost(new Uri(url), new BootStrapper());
            _nancyHost.Start();

            _logger.Info("Webserver started at " + url);

            if (enableAdmin && adminPort > 0) {
                adminUrl += "localhost:" + adminPort;
                _nancyAdminHost = new NancyHost(new Uri(adminUrl), new AdminBootStrapper());
                _nancyAdminHost.Start();

                _logger.Info("Admin webserver started at " + adminUrl);
            }
        }

        protected override void OnStop() {
            _nancyHost.Stop();
            if (_nancyAdminHost != null) {
                _nancyAdminHost.Stop();
            }

            _logger.Info("Service stopped.");
        }
    }
}
