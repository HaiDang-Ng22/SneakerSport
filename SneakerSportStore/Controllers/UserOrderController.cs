using SneakerSportStore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Web;
using System.IO;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace SneakerSportStore.Controllers
{
    public class UserOrderController : Controller
    {
        private const string FirebaseDbUrl = "https://sneakersportstore-default-rtdb.asia-southeast1.firebasedatabase.app/";
        private static readonly HttpClient httpClient = new HttpClient();

        public async Task<ActionResult> MyOrders()
        {
            string userId = Session["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            try
            {
                var response = await httpClient.GetAsync($"{FirebaseDbUrl}/orders.json");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var allOrders = JsonConvert.DeserializeObject<Dictionary<string, Order>>(json) ?? new Dictionary<string, Order>();

                // Lọc đơn hàng của người dùng hiện tại và gán OrderId
                var orders = allOrders
                    .Where(kvp => kvp.Value.UserId == userId)
                    .Select(kvp =>
                    {
                        var order = kvp.Value;
                        order.OrderId = kvp.Key;
                        return order;
                    })
                    .OrderByDescending(o => o.OrderDate)
                    .ToList();

                return View(orders);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Đã xảy ra lỗi: {ex.Message}";
                return View(new List<Order>());
            }
        }

        public async Task<ActionResult> OrderDetails(string orderId)
        {
            string userId = Session["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            try
            {
                var response = await httpClient.GetAsync($"{FirebaseDbUrl}/orders/{orderId}.json");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var order = JsonConvert.DeserializeObject<Order>(json);

                if (order == null || order.UserId != userId)
                {
                    TempData["ErrorMessage"] = "Đơn hàng không tồn tại hoặc không phải của bạn!";
                    return RedirectToAction("MyOrders");
                }

                // Gán OrderId và kiểm tra điều kiện hoàn trả
                order.OrderId = orderId;
                ViewBag.CanReturn = CanReturnOrder(order);

                return View(order);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Đã xảy ra lỗi: {ex.Message}";
                return RedirectToAction("MyOrders");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CancelOrder(string orderId)
        {
            string userId = Session["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            try
            {
                var response = await httpClient.GetAsync($"{FirebaseDbUrl}/orders/{orderId}.json");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var order = JsonConvert.DeserializeObject<Order>(json);

                if (order == null || order.UserId != userId)
                {
                    TempData["ErrorMessage"] = "Không thể hủy đơn hàng này!";
                    return RedirectToAction("MyOrders");
                }

                // Kiểm tra điều kiện hủy đơn
                if (!CanCancelOrder(order))
                {
                    TempData["ErrorMessage"] = "Không thể hủy đơn hàng ở trạng thái hiện tại!";
                    return RedirectToAction("OrderDetails", new { orderId });
                }

                order.Status = "Hủy";
                order.CancelReason = "Người dùng yêu cầu";
                order.CancelDate = DateTime.Now;

                var content = new StringContent(JsonConvert.SerializeObject(order), Encoding.UTF8, "application/json");
                var updateResponse = await httpClient.PutAsync($"{FirebaseDbUrl}/orders/{orderId}.json", content);

                if (updateResponse.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Đơn hàng đã được hủy thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Đã xảy ra lỗi khi hủy đơn hàng!";
                }

                return RedirectToAction("OrderDetails", new { orderId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Đã xảy ra lỗi: {ex.Message}";
                return RedirectToAction("OrderDetails", new { orderId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ReturnOrder(string orderId, string reason)
        {
            string userId = Session["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            try
            {
                var response = await httpClient.GetAsync($"{FirebaseDbUrl}/orders/{orderId}.json");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var order = JsonConvert.DeserializeObject<Order>(json);

                if (order == null || order.UserId != userId)
                {
                    TempData["ErrorMessage"] = "Không thể yêu cầu hoàn trả đơn hàng này!";
                    return RedirectToAction("MyOrders");
                }

                // Kiểm tra điều kiện hoàn trả
                if (!CanReturnOrder(order))
                {
                    TempData["ErrorMessage"] = "Đơn hàng không đủ điều kiện để hoàn trả!";
                    return RedirectToAction("OrderDetails", new { orderId });
                }

                // ✅ CẬP NHẬT ĐÚNG TRẠNG THÁI
                order.Status = "Yêu cầu hoàn trả";
                order.ReturnStatus = "Chờ xử lý";
                order.ReturnRequestDate = DateTime.Now;
                order.ReturnReason = reason;

                var content = new StringContent(JsonConvert.SerializeObject(order), Encoding.UTF8, "application/json");
                var updateResponse = await httpClient.PutAsync($"{FirebaseDbUrl}/orders/{orderId}.json", content);

                if (updateResponse.IsSuccessStatusCode)
                {
                    // ✅ Gửi thông báo cho admin
                    await SendReturnNotificationToAdmin(orderId);
                    TempData["SuccessMessage"] = "Yêu cầu hoàn trả đã được gửi thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Đã xảy ra lỗi khi gửi yêu cầu hoàn trả!";
                }

                return RedirectToAction("OrderDetails", new { orderId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Đã xảy ra lỗi: {ex.Message}";
                return RedirectToAction("OrderDetails", new { orderId });
            }
        }

        private bool CanCancelOrder(Order order)
        {
         
            return order.Status == "Chờ xác nhận" ||
                   order.Status == "Xác nhận" ||
                   order.Status == "Đang chuẩn bị";
        }
        private bool CanReturnOrder(Order order)
        {
            // Kiểm tra trạng thái: cả "Hoàn thành" và "Đã giao"
            if (order.Status != "Hoàn thành" && order.Status != "Đã giao")
                return false;

            // Kiểm tra ngày giao hàng
            if (!order.DeliveryDate.HasValue)
                return false;

            // Kiểm tra trong vòng 7 ngày
            var daysSinceDelivery = (DateTime.Now - order.DeliveryDate.Value).TotalDays;
            if (daysSinceDelivery > 7)
                return false;

            // Kiểm tra chưa có yêu cầu hoàn trả nào
            return string.IsNullOrEmpty(order.ReturnStatus);
        }

        //private bool CanReturnOrder(Order order)
        //{
        //    // Debug log
        //    System.Diagnostics.Debug.WriteLine($"Check return: Status={order.Status}, DeliveryDate={order.DeliveryDate}, Days={(order.DeliveryDate.HasValue ? (DateTime.Now - order.DeliveryDate.Value).TotalDays : -1)}");

        //    return order.Status == "Hoàn thành" &&
        //           order.DeliveryDate.HasValue &&
        //           (DateTime.Now - order.DeliveryDate.Value).TotalDays <= 7;
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RequestReturn(string orderId, string returnReason, IEnumerable<HttpPostedFileBase> returnImages)
        {
            string userId = Session["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            try
            {
                // Lấy thông tin đơn hàng
                var response = await httpClient.GetAsync($"{FirebaseDbUrl}/orders/{orderId}.json");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var order = JsonConvert.DeserializeObject<Order>(json);

                if (order == null || order.UserId != userId)
                {
                    TempData["ErrorMessage"] = "Đơn hàng không tồn tại!";
                    return RedirectToAction("MyOrders");
                }

                // Kiểm tra điều kiện hoàn hàng
                if (!CanReturnOrder(order))
                {
                    TempData["ErrorMessage"] = "Đơn hàng không đủ điều kiện hoàn hàng!";
                    return RedirectToAction("OrderDetails", new { orderId });
                }

                // Cập nhật thông tin hoàn hàng
                order.ReturnStatus = "Pending";
                order.ReturnReason = returnReason;
                order.ReturnRequestDate = DateTime.Now;
                order.ReturnImages = new List<string>();

                // Xử lý upload hình ảnh
                if (returnImages != null)
                {
                    foreach (var image in returnImages.Take(3)) // Giới hạn 3 ảnh
                    {
                        if (image != null && image.ContentLength > 0)
                        {
                            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                            var path = Path.Combine(Server.MapPath("~/Content/ReturnImages"), fileName);

                            // Đảm bảo thư mục tồn tại
                            Directory.CreateDirectory(Path.GetDirectoryName(path));
                            image.SaveAs(path);

                            order.ReturnImages.Add("/Content/ReturnImages/" + fileName);
                        }
                    }
                }
                var content = new StringContent(JsonConvert.SerializeObject(order), Encoding.UTF8, "application/json");
                var updateResponse = await httpClient.PutAsync($"{FirebaseDbUrl}/orders/{orderId}.json", content);

                if (updateResponse.IsSuccessStatusCode)
                {
                    await SendReturnNotificationToAdmin(orderId);
                    TempData["SuccessMessage"] = "Yêu cầu hoàn hàng đã được gửi thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Đã xảy ra lỗi khi gửi yêu cầu hoàn hàng!";
                }

                return RedirectToAction("OrderDetails", new { orderId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi gửi yêu cầu: {ex.Message}";
                return RedirectToAction("OrderDetails", new { orderId });
            }
        }
        // SneakerSportStore/Controllers/UserOrderController.cs

        private async Task SendReturnNotificationToAdmin(string orderId)
        {
            // Lấy danh sách tất cả admin
            var adminUserIds = await GetAdminUserIds();

            // Tạo thông báo
            var notification = new Notification
            {
                Id = Guid.NewGuid().ToString(),
                Message = $"Yêu cầu hoàn hàng mới cho đơn hàng #{orderId}",
                CreatedAt = DateTime.Now,
                IsRead = false,
                Type = "return",
                RelatedOrderId = orderId,
                RedirectUrl = Url.Action("Details", "AdminOrder", new { id = orderId }, Request.Url.Scheme)
            };

            // Gửi thông báo đến từng admin
            using (var client = new HttpClient())
            {
                var jsonNotification = JsonConvert.SerializeObject(notification);
                var content = new StringContent(jsonNotification, Encoding.UTF8, "application/json");

                foreach (var adminId in adminUserIds)
                {
                    await client.PostAsync($"{FirebaseDbUrl}/notifications/{adminId}/{notification.Id}.json", content);
                }
            }
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
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(json);

                    if (dict != null)
                    {
                        foreach (var user in dict)
                        {
                            // Kiểm tra role của người dùng
                            var role = user.Value["userRole"]?.ToString();
                            if (role == "Admin")
                            {
                                adminUserIds.Add(user.Key);
                            }
                        }
                    }
                }
            }

            return adminUserIds;
        }



    }
}