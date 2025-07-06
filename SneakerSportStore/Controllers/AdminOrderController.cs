using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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

        // Trang hiển thị tất cả các đơn hàng của Admin
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

        // Chi tiết đơn hàng của người dùng
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
                            if (order.Status == "Hủy")  // Kiểm tra trạng thái đã hủy
                            {
                                TempData["ErrorMessage"] = "Đơn hàng đã bị hủy, không thể cập nhật trạng thái!";
                                return RedirectToAction("Index");
                            }

                            order.Status = status; // Cập nhật trạng thái
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

    }
}
