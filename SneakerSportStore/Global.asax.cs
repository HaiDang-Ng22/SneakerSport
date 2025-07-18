//using System;
//using System.Web.Mvc;
//using System.Web.Routing;
//using Unity;
//using Unity.Lifetime;
//using Unity.Mvc5; // Or Unity.AspNet.Mvc depending on the package you're using
//using System.Net.Http;
//using System.Configuration;
//using SneakerSportStore.Models;
//using System.Web.Optimization;
//using System.Security.Principal;
//using System.Web.Security;
//using System.Web;

//namespace SneakerSportStore
//{
//    public class MvcApplication : System.Web.HttpApplication
//    {
//        protected void Application_Start()
//        {
//            AreaRegistration.RegisterAllAreas();
//            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
//            RouteConfig.RegisterRoutes(RouteTable.Routes);
//            BundleConfig.RegisterBundles(BundleTable.Bundles);

//            // Create Unity container
//            var container = new UnityContainer();

//            // Register HttpClient and settings classes
//            container.RegisterType<HttpClient>(new ContainerControlledLifetimeManager());

//            container.RegisterInstance(new FirebaseSettings
//            {
//                DbUrl = ConfigurationManager.AppSettings["FirebaseDbUrl"],
//                ApiKey = ConfigurationManager.AppSettings["FirebaseApiKey"]
//            });

//            //container.RegisterInstance(new TwilioSettings
//            //{
//            //    AccountSid = ConfigurationManager.AppSettings["TwilioAccountSid"],
//            //    AuthToken = ConfigurationManager.AppSettings["TwilioAuthToken"],
//            //    PhoneNumber = ConfigurationManager.AppSettings["TwilioPhoneNumber"]
//            //});

//            // Set the Unity resolver for MVC
//            DependencyResolver.SetResolver(new Unity.Mvc5.UnityDependencyResolver(container)); // Or Unity.AspNet.Mvc
//        }
//        protected void Application_PostAuthenticateRequest(object sender, EventArgs e)
//        {
//            // Chỉ xử lý khi request đã được xác thực
//            if (Context.Request.IsAuthenticated)
//            {
//                // Tạo identity từ thông tin xác thực
//                var identity = new GenericIdentity(User.Identity.Name);

//                // Lấy roles từ FormsAuthenticationTicket
//                var roles = new string[0];

//                if (User.Identity is FormsIdentity formsIdentity)
//                {
//                    var userData = formsIdentity.Ticket.UserData;
//                    if (!string.IsNullOrEmpty(userData))
//                    {
//                        roles = userData.Split(',');
//                    }
//                }

//                // Tạo principal mới với roles
//                var principal = new GenericPrincipal(identity, roles);
//                HttpContext.Current.User = principal;
//            }
//        }
//    }
//}

// Global.asax.cs
using System.Security.Principal;
using System.Web;
using System;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;

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

            // Không cần thêm gì ở đây, OWIN sẽ tự động khởi động
        }
        protected void Application_PostAuthenticateRequest(object sender, EventArgs e)
        {
            // Chỉ xử lý khi request đã được xác thực
            if (Context.Request.IsAuthenticated && User.Identity is FormsIdentity)
            {
                // Lấy identity hiện tại
                var identity = (FormsIdentity)User.Identity;

                // Lấy roles từ FormsAuthenticationTicket.UserData
                var roles = identity.Ticket.UserData.Split(',');

                // Tạo principal mới với roles
                var principal = new GenericPrincipal(identity, roles);
                HttpContext.Current.User = principal;
            }
        }
    }
}