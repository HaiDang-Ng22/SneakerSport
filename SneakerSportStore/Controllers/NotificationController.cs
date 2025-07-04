﻿using SneakerSportStore.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using System.Linq;
using SneakerSportStore.Extensions;
using System.Text;

namespace SneakerSportStore.Controllers
{
    public class NotificationController : Controller
    {
        private readonly string FirebaseDbUrl = "https://sneakersportstore-default-rtdb.asia-southeast1.firebasedatabase.app/";

        public async Task<ActionResult> Index()
        {
            string userId = Session["CustomerID"] as string;
            ViewBag.DebugUserId = userId;
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            List<Notification> notifications = new List<Notification>();
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(FirebaseDbUrl + $"/notifications/{userId}.json");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, Notification>>(json);
                    if (dict != null)
                        notifications = dict.Values.OrderByDescending(x => x.CreatedAt).ToList();
                }
                else
                {
                    ViewBag.Error = "Không thể kết nối tới máy chủ thông báo!";
                }
            }
            return View(notifications);
        }

        // Đánh dấu là đã đọc (có thể mở rộng)
        [HttpPost]
        public async Task<ActionResult> MarkAsRead(string notificationId)
        {
            string userId = Session["CustomerID"] as string;
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false });

            using (var client = new HttpClient())
            {
                var response = await client.PatchAsync(
                    FirebaseDbUrl + $"/notifications/{userId}/{notificationId}.json",
                    new StringContent("{\"IsRead\":true}", Encoding.UTF8, "application/json")
                );
            }
            return Json(new { success = true });
        }
        // NotificationController (action)
        public async Task<int> GetUnreadCount()
        {
            string userId = Session["CustomerID"] as string;
            if (string.IsNullOrEmpty(userId)) return 0;
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(FirebaseDbUrl + $"/notifications/{userId}.json");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, Notification>>(json);
                    if (dict != null)
                        return dict.Values.Count(x => !x.IsRead);
                }
            }
            return 0;
        }
        [ChildActionOnly]
        public ActionResult UnreadBadge()
        {
            int count = 0;
            string userId = Session["CustomerID"] as string;
            if (!string.IsNullOrEmpty(userId))
            {
                using (var client = new HttpClient())
                {
                    var response = client.GetAsync(FirebaseDbUrl + $"/notifications/{userId}.json").Result;
                    if (response.IsSuccessStatusCode)
                    {
                        var json = response.Content.ReadAsStringAsync().Result;
                        var dict = JsonConvert.DeserializeObject<Dictionary<string, Notification>>(json);
                        if (dict != null)
                            count = dict.Values.Count(x => !x.IsRead);
                    }
                }
            }
            ViewBag.Count = count;
            return PartialView("_NotificationBadge");
        }

    }
}
