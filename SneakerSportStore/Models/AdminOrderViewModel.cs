using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SneakerSportStore.Models
{
    public class AdminOrderViewModel
    {
        public List<Order> Orders { get; set; }
        public string SearchTerm { get; set; }
        public string StatusFilter { get; set; }
        public string SortOrder { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalOrders { get; set; }
        public List<SelectListItem> StatusOptions { get; set; }
    }
}