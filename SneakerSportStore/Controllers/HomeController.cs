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
using System.Web;

namespace SneakerSportStore.Controllers
{
    public class HomeController : Controller
    {
        private static readonly HttpClient client = new HttpClient()
        {
            BaseAddress = new Uri("https://sneakersportstore-default-rtdb.asia-southeast1.firebasedatabase.app/"),
            Timeout = TimeSpan.FromSeconds(30)
        };

        public async Task<ActionResult> Details(string id, string commentId = null)
        {
            if (string.IsNullOrEmpty(id))
                return HttpNotFound("Sản phẩm không tồn tại.");

            try
            {
                var productTask = GetProduct(id);
                var commentsTask = GetComments(id);

                await Task.WhenAll(productTask, commentsTask);

                var product = await productTask;
                var comments = await commentsTask;

                if (product == null)
                    return HttpNotFound("Sản phẩm không tồn tại.");

                ViewBag.SelectedComment = !string.IsNullOrEmpty(commentId)
                    ? comments?.FirstOrDefault(c => c.CommentId == commentId)
                    : null;
                ViewBag.UserId = Session["UserId"]?.ToString();
                ViewBag.UserRole = Session["UserRole"]?.ToString();
                ViewBag.Comments = comments;
                ViewBag.ProductId = id;
                ViewBag.HighlightCommentId = commentId; 
                return View(product);
            }
            catch (Exception ex)
            {
      
                return View("Error", new HandleErrorInfo(ex, "Home", "Details"));
            }
        }

        private async Task<ProductFireBaseKey> GetProduct(string id)
        {
            var response = await client.GetAsync($"products/{id}.json");
            return response.IsSuccessStatusCode
                ? JsonConvert.DeserializeObject<ProductFireBaseKey>(await response.Content.ReadAsStringAsync())
                : null;
        }

        private async Task<List<Comment>> GetComments(string productId)
        {
            var response = await client.GetAsync($"comments/{productId}.json");
            if (!response.IsSuccessStatusCode) return new List<Comment>();

            var json = await response.Content.ReadAsStringAsync();
            var dict = JsonConvert.DeserializeObject<Dictionary<string, Comment>>(json);

            return dict?.Values
                .OrderBy(x => x.CreatedAt)
                .ToList() ?? new List<Comment>();
        }

        [HttpPost]
        public async Task<ActionResult> AddComment(string productId, string content, string parentCommentId = null)
        {
            var userId = Session["UserId"]?.ToString();
            var userName = Session["FullName"]?.ToString();

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

            // Save comment
            var contentJson = new StringContent(JsonConvert.SerializeObject(comment), Encoding.UTF8, "application/json");
            await client.PutAsync($"comments/{productId}/{comment.CommentId}.json", contentJson);

            // Notify all admins
            var adminIds = await GetAdminUserIds();
            foreach (var adminId in adminIds)
            {
                await AddNotification(
                    userId: adminId,
                    message: $"{userName} đã bình luận về sản phẩm.",
                    productId: productId,
                    commentId: comment.CommentId
                );
            }

            return RedirectToAction("Details", new { id = productId });
        }

        private async Task<List<string>> GetAdminUserIds()
        {
            try
            {
                var response = await client.GetAsync("users.json");
                if (!response.IsSuccessStatusCode) return new List<string>();

                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json) || json == "null")
                    return new List<string>();

                var users = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(json);
                return users?
                    .Where(u => u.Value["userRole"]?.ToString() == "Admin")
                    .Select(u => u.Key)
                    .ToList() ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        private async Task AddNotification(string userId, string message, string productId, string commentId = null)
        {
            string redirectUrl = Url.Action("Details", "Home",
                new { id = productId, commentId = commentId },
                protocol: Request.Url.Scheme
            );

            var notification = new Notification
            {
                NotificationId = Guid.NewGuid().ToString(),
                UserId = userId,
                Message = $"Bình luận mới: {message}",
                ProductId = productId,
                CommentId = commentId,
                IsRead = false,
                CreatedAt = DateTime.Now,
                RedirectUrl = redirectUrl
            };

            var contentJson = new StringContent(JsonConvert.SerializeObject(notification), Encoding.UTF8, "application/json");
            await client.PutAsync($"notifications/{userId}/{notification.NotificationId}.json", contentJson);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteComment(string productId, string commentId)
        {
            var userId = Session["UserId"]?.ToString();
            var userRole = Session["UserRole"]?.ToString();

            if (string.IsNullOrEmpty(userId))
                return new HttpStatusCodeResult(403, "Bạn cần đăng nhập");

            try
            {
                // Kiểm tra quyền
                var comment = await GetComment(productId, commentId);
                if (comment == null)
                    return HttpNotFound("Bình luận không tồn tại");

                if (comment.UserId != userId && userRole != "Admin")
                    return new HttpStatusCodeResult(403, "Không có quyền xóa");

                // Xóa bình luận
                await client.DeleteAsync($"comments/{productId}/{commentId}.json");
                return RedirectToAction("Details", new { id = productId });
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(500, "Lỗi khi xóa: " + ex.Message);
            }
        }

        private async Task<Comment> GetComment(string productId, string commentId)
        {
            var response = await client.GetAsync($"comments/{productId}/{commentId}.json");
            return response.IsSuccessStatusCode
                ? JsonConvert.DeserializeObject<Comment>(await response.Content.ReadAsStringAsync())
                : null;
        }

        public async Task<ActionResult> Index(string searchTerm = "")
        {
            List<ProductFireBaseKey> products = new List<ProductFireBaseKey>();

            var response = await client.GetAsync("products.json");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var dict = JsonConvert.DeserializeObject<Dictionary<string, ProductFireBaseKey>>(json);

                if (dict != null)
                {
                    foreach (var item in dict)
                    {
                        item.Value.GiayId = item.Key;
                        products.Add(item.Value);
                    }
                }
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                products = products
                    .Where(g => g.TenGiay != null &&
                           g.TenGiay.ToLower().Contains(searchTerm.ToLower()))
                    .ToList();
            }

            ViewBag.SearchTerm = searchTerm;
            return View("Index", products);
        }

        public ActionResult Guide() => View();
        public ActionResult About() => View();
    }
}