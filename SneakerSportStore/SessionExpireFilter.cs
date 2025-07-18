using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;

namespace SneakerSportStore
{
    public class SessionExpireFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var ctx = filterContext.HttpContext;

            // Kiểm tra nếu session bị mất nhưng cookie xác thực vẫn còn
            if (ctx.Session != null && ctx.Session.IsNewSession)
            {
                string authCookie = ctx.Request.Headers["Cookie"];

                if (authCookie != null && authCookie.Contains("ASP.NET_SessionId"))
                {
                    FormsAuthentication.SignOut();
                    ctx.Session.Abandon();

                    filterContext.Result = new RedirectToRouteResult(
                        new RouteValueDictionary {
                        { "controller", "Account" },
                        { "action", "Login" }
                        }
                    );
                }
            }

            base.OnActionExecuting(filterContext);
        }
    }
}