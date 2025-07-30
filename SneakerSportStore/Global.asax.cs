using System;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using System.Diagnostics;

namespace SneakerSportStore
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        protected void Application_PostAuthenticateRequest(object sender, EventArgs e)
        {
            var authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];
            if (authCookie == null) return;

            try
            {
                var ticket = FormsAuthentication.Decrypt(authCookie.Value);
                if (ticket == null || ticket.Expired) return;

                var identity = new FormsIdentity(ticket);
                var roles = ticket.UserData.Split(',');
                var principal = new GenericPrincipal(identity, roles);

                HttpContext.Current.User = principal;
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Authentication error: {ex}");
                // Xóa cookie không hợp lệ
                Response.Cookies.Remove(FormsAuthentication.FormsCookieName);
                FormsAuthentication.SignOut();
            }
        }

        protected void Application_EndRequest(object sender, EventArgs e)
        {
            if (Context.Response.StatusCode != 401) return;
            if (Request.Url.AbsolutePath.Equals(FormsAuthentication.LoginUrl,
                  StringComparison.OrdinalIgnoreCase)) return;

            // Redirect về trang login kèm returnUrl
            var returnUrl = HttpUtility.UrlEncode(Request.Url.PathAndQuery);
            Response.Clear();
            Response.Redirect($"{FormsAuthentication.LoginUrl}?returnUrl={returnUrl}");
        }

    }
}