using SneakerSportStore.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using System.Linq;
using System.Text;

namespace SneakerSportStore.Controllers
{
    public class UserOrderController : Controller
    {
        private readonly string FirebaseDbUrl = "https://sneakersportstore-default-rtdb.asia-southeast1.firebasedatabase.app/";

        // Lấy các đơn hàng của người dùng
        public async Task<ActionResult> MyOrders()
        {
            string userId = Session["CustomerID"] as string;
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            List<Order> orders = new List<Order>();
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync($"{FirebaseDbUrl}/orders.json");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var allOrders = JsonConvert.DeserializeObject<Dictionary<string, Order>>(json);

                    if (allOrders != null)
                    {
                        orders = allOrders.Values.Where(o => o.UserId == userId).ToList();
                    }
                }
            }

            return View(orders);
        }

        // Xem chi tiết đơn hàng
        public async Task<ActionResult> OrderDetails(string orderId)
        {
            string userId = Session["CustomerID"] as string;
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            Order order = null;
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync($"{FirebaseDbUrl}/orders/{orderId}.json");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    order = JsonConvert.DeserializeObject<Order>(json);
                }
            }

            // Kiểm tra nếu đơn hàng không thuộc về người dùng hoặc không tìm thấy
            if (order == null || order.UserId != userId)
            {
                TempData["ErrorMessage"] = "Đơn hàng không tồn tại hoặc không phải của bạn!";
                return RedirectToAction("MyOrders");
            }

            return View(order);
        }

        [HttpPost]
        public async Task<ActionResult> CancelOrder(string orderId)
        {
            string userId = Session["CustomerID"] as string;
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            Order order = null;
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync($"{FirebaseDbUrl}/orders/{orderId}.json");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    order = JsonConvert.DeserializeObject<Order>(json);
                }
            }

            // Kiểm tra nếu đơn hàng không thuộc về người dùng hoặc đã hủy
            if (order == null || order.UserId != userId || order.Status == "Hủy")
            {
                TempData["ErrorMessage"] = "Không thể hủy đơn hàng này!";
                return RedirectToAction("MyOrders");
            }

            // Cập nhật trạng thái đơn hàng thành "Hủy"
            order.Status = "Hủy";

            using (var client = new HttpClient())
            {
                var content = new StringContent(JsonConvert.SerializeObject(order), Encoding.UTF8, "application/json");
                var updateResponse = await client.PutAsync($"{FirebaseDbUrl}/orders/{orderId}.json", content);

                if (updateResponse.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Đơn hàng đã được hủy thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Đã xảy ra lỗi khi hủy đơn hàng!";
                }
            }

            return RedirectToAction("MyOrders");
        }
    }
}
