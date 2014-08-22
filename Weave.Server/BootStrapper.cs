/*
Weave Server.NET <http://code.google.com/p/weave-server-dotnet/>
Copyright (C) 2014 Karoly Lukacs

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
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using NLog;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Diagnostics;
using Nancy.TinyIoc;
using Weave.Core;
using Weave.Core.Models;

namespace Weave.Server {
	public class BootStrapper : DefaultNancyBootstrapper {
		private static Logger _logger = LogManager.GetCurrentClassLogger();
		WeaveConfigurationSection _config = (WeaveConfigurationSection)ConfigurationManager.GetSection("weave");

		protected override byte[] FavIcon {
			get { return null; }
		}

		// Register only NancyModules found in this assembly
		protected override IEnumerable<ModuleRegistration> Modules {
			get {
				return GetType().Assembly.GetTypes().Where(type => type.BaseType == typeof(NancyModule))
				       .NotOfType<DiagnosticModule>()
				       .Select(t => new ModuleRegistration(t)).ToArray();
			}
		}

		protected override NancyInternalConfiguration InternalConfiguration {
			get {
				return NancyInternalConfiguration.WithOverrides(OnConfigurationBuilder);
			}
		}

		protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines) {
			if (!_config.EnableDebug) {
				DiagnosticsHook.Disable(pipelines);
			}

			_logger.Info("Weave webserver started.");
		}

		protected override void RequestStartup(TinyIoCContainer container, IPipelines pipelines, NancyContext context) {
			base.RequestStartup(container, pipelines, context);

			pipelines.BeforeRequest.AddItemToStartOfPipeline(c => {
				_logger.Trace("Request {0} {1}", c.Request.Method, c.Request.Url);
				return c.Response;
			});

			pipelines.AfterRequest.AddItemToEndOfPipeline(c => _logger.Trace("Response {0} {1}", c.Response.StatusCode, c.Response.ContentType));
		}

		void OnConfigurationBuilder(NancyInternalConfiguration conf) {
			conf.StatusCodeHandlers = new List<Type> { typeof(StatusCodeHandler) };
		}

		protected override DiagnosticsConfiguration DiagnosticsConfiguration {
			get { return new DiagnosticsConfiguration { Password = _config.DiagPassword }; }
		}
	}
}
