﻿using System.Web.Mvc;
using System.Web.Routing;

namespace WeaveServerNET {
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication {
        public static void RegisterRoutes(RouteCollection routes) {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                "Admin",
                "Admin/{action}",
                new { controller = "Admin", action = "Index" }
            );

            routes.MapRoute(
                "DeleteUser",
                "DeleteUser",
                new { controller = "Admin", action = "DeleteUser" }
            );

            routes.MapRoute(
                "Weave",
                "{param1}/{param2}/{param3}/{param4}/{param5}",
                new { controller = "Weave", action = "Index", param1 = UrlParameter.Optional, 
                                                              param2 = UrlParameter.Optional, 
                                                              param3 = UrlParameter.Optional, 
                                                              param4 = UrlParameter.Optional, 
                                                              param5 = UrlParameter.Optional }
            );
        }

        protected void Application_Start() {
            AreaRegistration.RegisterAllAreas();

            RegisterRoutes(RouteTable.Routes);
        }
    }
}