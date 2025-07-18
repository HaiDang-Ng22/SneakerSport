using SneakerSportStore.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using System.Linq;
using System.Text;
using SneakerSportStore.Extensions;
using System;
using Newtonsoft.Json.Linq;

namespace SneakerSportStore.Controllers
{
    public class NotificationController : Controller
    {
        private readonly string FirebaseDbUrl = "https://sneakersportstore-default-rtdb.asia-southeast1.firebasedatabase.app/";

        // SneakerSportStore/Controllers/NotificationController.cs

        public async Task<ActionResult> Index()
        {
            string userId = Session["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            List<Notification> notifications = new List<Notification>();
            using (var client = new HttpClient())
            {
                // Sửa URL để lấy thông báo cho user cụ thể
                var response = await client.GetAsync(FirebaseDbUrl + $"/notifications/{userId}.json");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(json) && json != "null")
                    {
                        try
                        {
                            // Đọc dưới dạng dictionary các thông báo
                            var notificationsDict = JsonConvert.DeserializeObject<Dictionary<string, Notification>>(json);

                            if (notificationsDict != null)
                            {
                                notifications = notificationsDict.Values
                                    .OrderByDescending(n => n.CreatedAt)
                                    .ToList();
                            }
                        }
                        catch (JsonSerializationException ex)
                        {
                            ViewBag.Error = "Lỗi khi tải thông báo: " + ex.Message;
                        }
                    }
                }
            }

            return View(notifications);
        }
        [HttpPost]
        public async Task<ActionResult> MarkAsRead(string notificationId)
        {
            string userId = Session["UserId"] as string;
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
        public async Task<int> GetUnreadCount()
        {
            string userId = Session["UserId"] as string;
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
            string userId = Session["UserId"] as string;
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
        [HttpPost]
        public async Task<ActionResult> Delete(string firebaseKey)
        {
            var userId = Session["UserId"]?.ToString();
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            using (var client = new HttpClient())
            {
                var response = await client.DeleteAsync($"{FirebaseDbUrl}/notifications/{userId}/{firebaseKey}.json");
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Đã xóa !";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể xóa!";
                }
                return RedirectToAction("Index");
            }
        }


      
    }
}
