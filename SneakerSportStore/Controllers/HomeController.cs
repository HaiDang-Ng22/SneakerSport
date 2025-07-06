using SneakerSportStore.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using System.Text;
using System;
using Newtonsoft.Json.Linq;

namespace SneakerSportStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly string FirebaseDbUrl = "https://sneakersportstore-default-rtdb.asia-southeast1.firebasedatabase.app/";

        public async Task<ActionResult> Details(string id, string commentId = null)
        {
            Giay product = null;
            List<Comment> comments = new List<Comment>();

            using (var client = new HttpClient())
            {
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

                var cmtResponse = await client.GetAsync(FirebaseDbUrl + $"/comments/{id}.json");
                if (cmtResponse.IsSuccessStatusCode)
                {
                    var cmtJson = await cmtResponse.Content.ReadAsStringAsync();
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, Comment>>(cmtJson);
                    if (dict != null)
                        comments = dict.Values.OrderBy(x => x.CreatedAt).ToList();
                }
            }

            if (!string.IsNullOrEmpty(commentId))
            {
                var selectedComment = comments.FirstOrDefault(c => c.CommentId == commentId);
                ViewBag.SelectedComment = selectedComment;
            }

            ViewBag.Comments = comments;
            ViewBag.ProductId = id;
            return View(product);
        }




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

            using (var client = new HttpClient())
            {
                var contentJson = new StringContent(JsonConvert.SerializeObject(comment), Encoding.UTF8, "application/json");
                await client.PutAsync(FirebaseDbUrl + $"/comments/{productId}/{comment.CommentId}.json", contentJson);
            }

            var adminUserIds = await GetAdminUserIds();
            string message = $"Có comment mới ở sản phẩm: \"{content}\"";
            foreach (var adminId in adminUserIds)
            {
                await AddNotification(adminId, message, productId, comment.CommentId); 
            }

            return RedirectToAction("Details", new { id = productId });
        }



        private async Task<List<string>> GetAdminUserIds()
        {
            var adminUserIds = new List<string>();

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(FirebaseDbUrl + "/users.json");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();

                    // Kiểm tra dữ liệu JSON nhận được
                    Console.WriteLine("Received JSON: " + json);

                    // Deserialize thành Dictionary<string, dynamic> để dễ dàng xử lý
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(json);

                    if (dict != null)
                    {
                        foreach (var pair in dict)
                        {
                            // Kiểm tra xem đối tượng có phải là dynamic không và có trường "userRole"
                            var user = pair.Value as JObject; // Chuyển về JObject
                            if (user != null && user["userRole"]?.ToString() == "Admin")
                            {
                                adminUserIds.Add(pair.Key); // Lưu key là userId
                            }
                        }
                    }
                }
            }

            return adminUserIds;
        }


        private async Task SendOrderNotification(string orderId, string userId)
        {
            var adminNotification = new Notification
            {
                Message = $"Đơn hàng mới {orderId} của người dùng {userId} đang chờ xác nhận.",
                IsRead = false,
                CreatedAt = DateTime.Now,
                Type = "OrderUpdate",
                RelatedOrderId = orderId,
                RedirectUrl = Url.Action("Details", "AdminOrder", new { id = orderId }, protocol: Request.Url.Scheme) 
            };

            // Get all admin user IDs
            var adminUserIds = await GetAdminUserIds();

            using (var client = new HttpClient())
            {
                foreach (var adminId in adminUserIds)
                {
                    var jsonAdmin = JsonConvert.SerializeObject(adminNotification);
                    var contentAdmin = new StringContent(jsonAdmin, Encoding.UTF8, "application/json");

                    // Save the notification under the admin's notification node
                    await client.PostAsync(FirebaseDbUrl + $"/notifications/{adminId}.json", contentAdmin);
                }
            }
        }

        private async Task AddNotification(string userId, string message, string productId, string commentId = null, string orderId = null)
        {
            string redirectUrl = string.Empty;
            string notificationMessage = message;

            // Kiểm tra nếu là bình luận
            if (!string.IsNullOrEmpty(commentId))
            {
                redirectUrl = Url.Action("Details", "Home", new { id = productId, commentId = commentId }, protocol: Request.Url.Scheme);
                notificationMessage = $"Bình luận mới: {message}";  // Thêm thông tin loại thông báo
            }
            // Kiểm tra nếu là đơn hàng
            else if (!string.IsNullOrEmpty(orderId))
            {
                redirectUrl = Url.Action("Details", "AdminOrder", new { id = orderId }, protocol: Request.Url.Scheme);
                notificationMessage = $"Đơn hàng mới: {message}"; // Thêm thông tin loại thông báo
            }

            var notification = new Notification
            {
                NotificationId = Guid.NewGuid().ToString(),
                UserId = userId,
                Message = notificationMessage,  // Thông điệp đã chỉnh sửa
                ProductId = productId,
                CommentId = commentId,
                OrderId = orderId,
                IsRead = false,
                CreatedAt = DateTime.Now,
                RedirectUrl = redirectUrl
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
