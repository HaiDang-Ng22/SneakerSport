using SneakerSportStore.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;

namespace SneakerSportStore.Controllers
{
    public class CustomerController : Controller
    {
        private readonly string FirebaseDbUrl = "https://sneakersportstore-default-rtdb.asia-southeast1.firebasedatabase.app";

        // GET: Customer
        public async Task<ActionResult> Index()
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync($"{FirebaseDbUrl}/users.json");
                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Không thể tải dữ liệu khách hàng.";
                    return View(new List<KeyValuePair<string, KhachHang>>());
                }

                var json = await response.Content.ReadAsStringAsync();
                var dict = JsonConvert.DeserializeObject<Dictionary<string, KhachHang>>(json);

                var customerList = new List<KeyValuePair<string, KhachHang>>();
                if (dict != null)
                {
                    foreach (var entry in dict)
                    {
                        customerList.Add(new KeyValuePair<string, KhachHang>(entry.Key, entry.Value));
                    }
                }

                return View(customerList);
            }
        }

        // GET: Customer/Details/{id}
        public async Task<ActionResult> Details(string id)
        {
            using (var client = new HttpClient())
            {
                var userResponse = await client.GetAsync($"{FirebaseDbUrl}/users/{id}.json");
                if (!userResponse.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Không tìm thấy người dùng.";
                    return RedirectToAction("Index");
                }

                var userJson = await userResponse.Content.ReadAsStringAsync();
                var user = JsonConvert.DeserializeObject<KhachHang>(userJson);

                var orderResponse = await client.GetAsync($"{FirebaseDbUrl}/orders.json");
                var orders = new List<dynamic>();
                if (orderResponse.IsSuccessStatusCode)
                {
                    var ordersJson = await orderResponse.Content.ReadAsStringAsync();
                    var allOrders = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(ordersJson);

                    if (allOrders != null)
                    {
                        foreach (var item in allOrders)
                        {
                            if (item.Value.khachHangId == id)
                            {
                                orders.Add(item.Value);
                            }
                        }
                    }
                }

                ViewBag.Orders = orders;
                ViewBag.UserId = id;
                return View(user);
            }
        }

        // POST: Customer/Delete/{id}
        [HttpPost]
        public async Task<ActionResult> Delete(string id)
        {
            if (Session["UserRole"]?.ToString() != "Admin")
            {
                return new HttpUnauthorizedResult();
            }

            using (var client = new HttpClient())
            {
                var response = await client.DeleteAsync($"{FirebaseDbUrl}/users/{id}.json");
                if (response.IsSuccessStatusCode)
                {
                    TempData["Message"] = "Xóa tài khoản thành công.";
                }
                else
                {
                    TempData["Error"] = "Không thể xóa tài khoản.";
                }
                return RedirectToAction("Index");
            }
        }
    }
}
