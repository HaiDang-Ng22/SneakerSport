//using SneakerSportStore.Controllers;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Web;

//namespace SneakerSportStore.Models
//{
//    public class DashboardViewModel
//    {
//        // Thông tin chính
//        public UserInfo UserInfo { get; set; }

//        // Thống kê
//        public int OrderCount { get; set; }
//        public int UnreadNotifications { get; set; }
//        public decimal TotalSpent { get; set; }

//        // Hoạt động
//        public List<UserActivity> RecentActivities { get; set; }

//        // Đơn hàng gần đây
//        public List<OrderSummary> RecentOrders { get; set; }
//    }

  

//    public class UserActivity
//    {
//        public DateTime Timestamp { get; set; }
//        public string Action { get; set; } // "LOGIN", "UPDATE_PROFILE", "PURCHASE"
//        public string Details { get; set; }
//        public string IPAddress { get; set; }
//    }
//}