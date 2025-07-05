using SneakerSportStore.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using System.Text;
using System;

namespace SneakerSportStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly string FirebaseDbUrl = "https://sneakersportstore-default-rtdb.asia-southeast1.firebasedatabase.app/";

        // GET: /Home/Details/{id}
        public async Task<ActionResult> Details(string id)
        {
            Giay product = null;
            List<Comment> comments = new List<Comment>();

            using (var client = new HttpClient())
            {
                // Lấy sản phẩm
                var response = await client.GetAsync(FirebaseDbUrl + "/products/" + id + ".json");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    product = JsonConvert.DeserializeObject<Giay>(json);
                }
                else
                {
                    return HttpNotFound("Sản phẩm không tồn tại.");
                }

                // Lấy bình luận
                var cmtResponse = await client.GetAsync(FirebaseDbUrl + $"/comments/{id}.json");
                if (cmtResponse.IsSuccessStatusCode)
                {
                    var cmtJson = await cmtResponse.Content.ReadAsStringAsync();
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, Comment>>(cmtJson);
                    if (dict != null)
                        comments = dict.Values.OrderBy(x => x.CreatedAt).ToList();
                }
            }

            ViewBag.Comments = comments;
            ViewBag.ProductId = id;
            return View(product); // LUÔN truyền product!
        }

      

        private const string ADMIN_USER_ID = "SuNguyen13022005-24072024"; // UserId của admin

        [HttpPost]
        public async Task<ActionResult> AddComment(string productId, string content, string parentCommentId = null)
        {
            var userId = Session["CustomerID"]?.ToString();
            var userName = Session["Username"]?.ToString();

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var comment = new Comment
            {
                CommentId = Guid.NewGuid().ToString(),
                ProductId = productId,
                UserId = userId,
                UserName = userName,
                Content = content,
                ParentCommentId = parentCommentId,
                CreatedAt = DateTime.Now
            };

            // 1. Lưu comment vào Firebase như cũ
            using (var client = new HttpClient())
            {
                var contentJson = new StringContent(JsonConvert.SerializeObject(comment), Encoding.UTF8, "application/json");
                await client.PutAsync(FirebaseDbUrl + $"/comments/{productId}/{comment.CommentId}.json", contentJson);
            }

            // 2. Lấy danh sách tất cả user có role là Admin và gửi thông báo cho họ
            var adminUserIds = await GetAdminUserIds();
            string message = $"Có comment ở sản phẩm mới: \"{content}\"";

            foreach (var adminId in adminUserIds)
            {
                await AddNotification(adminId, message, productId, comment.CommentId);
            }

            return RedirectToAction("Details", new { id = productId });
        }

        // Hàm lấy danh sách tất cả userId có role là admin
        private async Task<List<string>> GetAdminUserIds()
        {
            var adminUserIds = new List<string>();
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(FirebaseDbUrl + "/users.json");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, ApplicationUser>>(json);
                    if (dict != null)
                    {
                        foreach (var pair in dict)
                        {
                            if (pair.Value.userRole == "Admin") // Chú ý: phải đúng property trong firebase (thường là userRole, không phải Role)
                                adminUserIds.Add(pair.Key); // userId là key trên firebase
                        }
                    }
                }
            }
            return adminUserIds;
        }

        // Hàm ghi notification cho từng admin
        private async Task AddNotification(string userId, string message, string productId, string commentId)
        {
            var notification = new Notification
            {
                NotificationId = Guid.NewGuid().ToString(),
                UserId = userId,
                Message = message,
                ProductId = productId,
                CommentId = commentId,
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            using (var client = new HttpClient())
            {
                var contentJson = new StringContent(JsonConvert.SerializeObject(notification), Encoding.UTF8, "application/json");
                await client.PutAsync(
                    FirebaseDbUrl + $"/notifications/{userId}/{notification.NotificationId}.json", contentJson
                );
            }
        }

        [HttpPost]
        public async Task<ActionResult> DeleteComment(string productId, string commentId)
        {
            var userId = Session["CustomerID"]?.ToString();
            var userRole = Session["UserRole"]?.ToString();

            Comment comment = null;
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(FirebaseDbUrl + $"/comments/{productId}/{commentId}.json");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    comment = JsonConvert.DeserializeObject<Comment>(json);
                }
            }
            if (comment == null || (comment.UserId != userId && userRole != "Admin"))
                return new HttpStatusCodeResult(403, "Không có quyền xóa!");

            using (var client = new HttpClient())
            {
                await client.DeleteAsync(FirebaseDbUrl + $"/comments/{productId}/{commentId}.json");
            }

            return RedirectToAction("Details", new { id = productId });
        }

        //private async Task AddNotification(string userId, string message, string productId, string commentId)
        //{
        //    var notification = new Notification
        //    {
        //        NotificationId = Guid.NewGuid().ToString(),
        //        UserId = userId,
        //        Message = message,
        //        ProductId = productId,
        //        CommentId = commentId,
        //        IsRead = false,
        //        CreatedAt = DateTime.Now
        //    };

        //    using (var client = new HttpClient())
        //    {
        //        var contentJson = new StringContent(JsonConvert.SerializeObject(notification), Encoding.UTF8, "application/json");
        //        await client.PutAsync(
        //            FirebaseDbUrl + $"/notifications/{userId}/{notification.NotificationId}.json", contentJson
        //        );
        //    }
        //}

        // ... Các hàm khác giữ nguyên ...
        public async Task<ActionResult> Index(string searchTerm = "")
        {
            List<Giay> products = new List<Giay>();

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(FirebaseDbUrl + "/products.json");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, Giay>>(json);
                    if (dict != null)
                    {
                        foreach (var item in dict)
                        {
                            item.Value.GiayId = item.Key;
                            products.Add(item.Value);
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                products = products.Where(g => g.TenGiay != null && g.TenGiay.ToLower().Contains(searchTerm.ToLower())).ToList();
            }

            ViewBag.SearchTerm = searchTerm;
            return View("SimpleIndex", products);
        }

        public ActionResult Guide() => View();
        public ActionResult About() => View();

        public class User
        {
            public string Username { get; set; }
            public string Role { get; set; }
        }
    }
}
