using System.Web;
using System.Web.Mvc;

namespace SneakerSportStore
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new SessionExpireFilter());
        }
    }
}
