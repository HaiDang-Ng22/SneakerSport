using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using SneakerSportStore.Models;

namespace SneakerSportStore.Controllers
{
    public class AdminOrderController : Controller
    {
        private readonly string FirebaseDbUrl = "https://sneakersportstore-default-rtdb.asia-southeast1.firebasedatabase.app/";
        
        public async Task<ActionResult> Index()
        {

            List<Order> orders = new List<Order>();

            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(FirebaseDbUrl + "/orders.json");

                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var dict = JsonConvert.DeserializeObject<Dictionary<string, Order>>(json);

                        if (dict != null)
                            orders = dict.Values.ToList();
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Không thể tải đơn hàng từ cơ sở dữ liệu.";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
            }

            return View(orders);
        }
     
        public async Task<ActionResult> Details(string id)
        {
            Order order = null;

            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(FirebaseDbUrl + $"/orders/{id}.json");

                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        order = JsonConvert.DeserializeObject<Order>(json);
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Không thể lấy thông tin đơn hàng.";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
            }

            return View(order);
        }
 
        [HttpPost]
        public async Task<ActionResult> UpdateDeliveryDate(string id, DateTime deliveryDate)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(FirebaseDbUrl + $"/orders/{id}.json");

                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var order = JsonConvert.DeserializeObject<Order>(json);

                        if (order != null)
                        {
                            order.DeliveryDate = deliveryDate;
                            var orderJson = JsonConvert.SerializeObject(order);
                            await client.PutAsync(FirebaseDbUrl + $"/orders/{id}.json",
                                                  new StringContent(orderJson, Encoding.UTF8, "application/json"));

                            TempData["SuccessMessage"] = "Ngày giao hàng đã được cập nhật!";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi cập nhật ngày giao hàng: " + ex.Message;
            }

            return RedirectToAction("Details", new { id });
        }
 
        [HttpPost]
        public async Task<ActionResult> UpdateOrderStatus(string id, string status)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(FirebaseDbUrl + $"/orders/{id}.json");

                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var order = JsonConvert.DeserializeObject<Order>(json);

                        if (order != null)
                        {
                            if (order.Status == "Hủy")
                            {
                                TempData["ErrorMessage"] = "Đơn hàng đã bị hủy, không thể cập nhật trạng thái!";
                                return RedirectToAction("Index");
                            }

                             if (status == "Hoàn thành" && !order.DeliveryDate.HasValue)
                            {
                                order.DeliveryDate = DateTime.Now;
                            }

                            order.Status = status;
                            var orderJson = JsonConvert.SerializeObject(order);
                            await client.PutAsync(FirebaseDbUrl + $"/orders/{id}.json", new StringContent(orderJson, Encoding.UTF8, "application/json"));

                            TempData["SuccessMessage"] = "Trạng thái đơn hàng đã được cập nhật!";
                        }
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Không thể cập nhật trạng thái đơn hàng.";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
            }

            return RedirectToAction("Index");
        }



        [HttpPost]
        public async Task<ActionResult> ProcessReturn(string id, string returnStatus, string adminComment)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    // Lấy thông tin đơn hàng
                    var response = await client.GetAsync($"{FirebaseDbUrl}/orders/{id}.json");
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync();
                    var order = JsonConvert.DeserializeObject<Order>(json);

                    if (order == null)
                    {
                        TempData["ErrorMessage"] = "Đơn hàng không tồn tại!";
                        return RedirectToAction("Index");
                    }

                    // Cập nhật trạng thái hoàn hàng
                    order.ReturnStatus = returnStatus;

                    // Thêm bình luận của admin
                    if (!string.IsNullOrEmpty(adminComment))
                    {
                        order.ReturnReason += $"\n\n[Admin]: {adminComment}";
                    }

                    // Cập nhật lên Firebase
                    var content = new StringContent(JsonConvert.SerializeObject(order),
                        Encoding.UTF8, "application/json");

                    await client.PutAsync($"{FirebaseDbUrl}/orders/{id}.json", content);

                    // Gửi thông báo cho người dùng
                    await SendReturnStatusNotification(order.UserId, id, returnStatus);

                    TempData["SuccessMessage"] = $"Đã cập nhật trạng thái hoàn hàng thành {returnStatus}!";
                    return RedirectToAction("Details", new { id });
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi: {ex.Message}";
                return RedirectToAction("Details", new { id });
            }
        }

        private async Task SendReturnStatusNotification(string userId, string orderId, string status)
        {
            var message = status == "Approved"
                ? "Yêu cầu hoàn hàng đã được chấp nhận"
                : "Yêu cầu hoàn hàng đã bị từ chối";

            var notification = new Notification
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Message = $"{message} cho đơn hàng #{orderId}",
                CreatedAt = DateTime.Now,
                IsRead = false,
                RelatedOrderId = orderId,
                RedirectUrl = Url.Action("OrderDetails", "UserOrder", new { orderId })
            };

            using (var client = new HttpClient())
            {
                var content = new StringContent(JsonConvert.SerializeObject(notification),
                    Encoding.UTF8, "application/json");

                await client.PostAsync($"{FirebaseDbUrl}/notifications/{userId}/{notification.Id}.json", content);
            }
        }


    }
         
}
