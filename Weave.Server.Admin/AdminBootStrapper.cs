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
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Bootstrapper;
using Nancy.Conventions;
using Nancy.Cryptography;
using Nancy.Diagnostics;
using Nancy.Responses;
using Nancy.TinyIoc;
using Nancy.ViewEngines;
using NLog;
using Weave.Core.Models;
using Weave.Server.Admin.Modules;

namespace Weave.Server.Admin {
	public class AdminBootStrapper : DefaultNancyBootstrapper {
		private static Logger _logger = LogManager.GetCurrentClassLogger();
		WeaveConfigurationSection _config = (WeaveConfigurationSection)ConfigurationManager.GetSection("weave");
		private byte[] _favicon;

		protected override byte[] FavIcon {
			get {
				return _favicon ?? (_favicon = LoadFavIcon());
			}
		}

		private byte[] LoadFavIcon() {
			using (var resourceStream = GetType().Assembly.GetManifestResourceStream("Weave.Server.Admin.Content.favicon.ico")) {
				if (resourceStream == null) {
					return null;
				}

				var tempFavicon = new byte[resourceStream.Length];
				resourceStream.Read(tempFavicon, 0, (int)resourceStream.Length);
				return tempFavicon;
			}
		}

		// Register only NancyModules found in this assembly
		protected override IEnumerable<ModuleRegistration> Modules {
			get {
				return GetType().Assembly.GetTypes().Where(type => type.BaseType == typeof(NancyModule))
				       .NotOfType<DiagnosticModule>()
				       .Select(t => new ModuleRegistration(t)).ToArray();
			}
		}

		protected override void ConfigureApplicationContainer(TinyIoCContainer container) {
			base.ConfigureApplicationContainer(container);

			ResourceViewLocationProvider.RootNamespaces.Add(Assembly.GetAssembly(typeof(LoginModule)), "Weave.Server.Admin.Views");
		}

		protected override void ConfigureConventions(NancyConventions conventions) {
			base.ConfigureConventions(conventions);

			conventions.StaticContentsConventions.Add(AddStaticResourcePath("/Content", Assembly.GetAssembly(typeof(LoginModule)), "Weave.Server.Admin.Content"));
			conventions.StaticContentsConventions.Add(AddStaticResourcePath("/Scripts", Assembly.GetAssembly(typeof(LoginModule)), "Weave.Server.Admin.Scripts"));
		}

		protected override NancyInternalConfiguration InternalConfiguration {
			get {
				return NancyInternalConfiguration.WithOverrides(OnConfigurationBuilder);
			}
		}

		protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines) {
			base.ApplicationStartup(container, pipelines);

			var formsAuthConfiguration = new FormsAuthenticationConfiguration();
			formsAuthConfiguration.RedirectUrl = "~/Login";
			formsAuthConfiguration.UserMapper = container.Resolve<IUserMapper>();

			if (!String.IsNullOrEmpty(_config.RijndaelPass) && !String.IsNullOrEmpty(_config.HmacPass)) {
				formsAuthConfiguration.CryptographyConfiguration = new CryptographyConfiguration(
				    new RijndaelEncryptionProvider(new PassphraseKeyGenerator(_config.RijndaelPass, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })),
				    new DefaultHmacProvider(new PassphraseKeyGenerator(_config.HmacPass, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })));
			}

			FormsAuthentication.Enable(pipelines, formsAuthConfiguration);

			if (!_config.EnableDebug) {
				DiagnosticsHook.Disable(pipelines);
			}

			_logger.Info("Weave admin webserver started.");
		}

		protected override void RequestStartup(TinyIoCContainer container, IPipelines pipelines, NancyContext context) {
			base.RequestStartup(container, pipelines, context);

			pipelines.BeforeRequest.AddItemToStartOfPipeline(c => {
				_logger.Trace("Request {0} {1}", c.Request.Method, c.Request.Url);
				return c.Response;
			});

			pipelines.AfterRequest.AddItemToEndOfPipeline(c => {
				_logger.Trace("Response {0} {1}", c.Response.StatusCode, c.Response.ContentType);
			});
		}

		void OnConfigurationBuilder(NancyInternalConfiguration conf) {
			conf.ViewLocationProvider = typeof(ResourceViewLocationProvider);
			conf.StatusCodeHandlers = new List<Type> { typeof(StatusCodeHandler) };
		}

		public static Func<NancyContext, string, Response> AddStaticResourcePath(string requestedPath, Assembly assembly, string namespacePrefix) {
			return (context, s) => {
				var path = context.Request.Path;
				if (!path.StartsWith(requestedPath)) {
					return null;
				}

				string resourcePath;
				string name;

				var adjustedPath = path.Substring(requestedPath.Length + 1);
				if (adjustedPath.IndexOf('/') >= 0) {
					name = Path.GetFileName(adjustedPath);
					resourcePath = namespacePrefix + "." + adjustedPath.Substring(0, adjustedPath.Length - name.Length - 1).Replace('/', '.');
				} else {
					name = adjustedPath;
					resourcePath = namespacePrefix;
				}

				return new EmbeddedFileResponse(assembly, resourcePath, name);
			};
		}

		protected override DiagnosticsConfiguration DiagnosticsConfiguration {
			get {
				return new DiagnosticsConfiguration { Password = _config.DiagPassword };
			}
		}
	}
}