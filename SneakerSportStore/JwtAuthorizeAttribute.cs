using SneakerSportStore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace SneakerSportStore
{
    public class JwtAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            var token = httpContext.Request.Cookies["AuthToken"]?.Value;
            if (string.IsNullOrEmpty(token)) return false;

            try
            {
                var principal = JwtTokenHelper.ValidateToken(token);
                httpContext.User = principal;
                return true;
            }
            catch
            {
                return false;
            }
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            filterContext.Result = new RedirectToRouteResult(
                new RouteValueDictionary {
                { "controller", "Account" },
                { "action", "Login" }
                });
        }
    }
}