using SneakerSportStore.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.UI.WebControls;

namespace SneakerSportStore
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
            //=================================================================


            routes.MapRoute(
                name: "UserChat",
                url: "chat",
                defaults: new { controller = "Chat", action = "UserChat" }
            );

            routes.MapRoute(
                name: "AdminChat",
                url: "admin/chat",
                defaults: new { controller = "Chat", action = "AdminChat" }
            );

            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.IgnoreRoute("Content/{*pathInfo}");
            routes.IgnoreRoute("Scripts/{*pathInfo}");
        }
    }

}
