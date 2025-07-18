using SneakerSportStore.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using System;

namespace SneakerSportStore.Controllers
{
    public class CustomerController : Controller
    {
        private readonly string FirebaseDbUrl = "https://sneakersportstore-default-rtdb.asia-southeast1.firebasedatabase.app";

        public async Task<ActionResult> Index(string searchString, string roleFilter)
        {
            // Check if user is admin
            if (Session["UserId"] != null && Session["UserRole"]?.ToString() != "Admin")
            {
                TempData["Error"] = "Bạn không có quyền truy cập trang này";
                return RedirectToAction("Index", "Home");
            }

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
                    var query = dict.AsQueryable();

                    if (!string.IsNullOrEmpty(searchString))
                    {
                        string lowerSearch = searchString.ToLower();
                        query = query.Where(x =>
                            (x.Value.HoTen != null && x.Value.HoTen.ToLower().Contains(lowerSearch)) ||
                            (x.Value.Email != null && x.Value.Email.ToLower().Contains(lowerSearch)) ||
                            (x.Value.TenDangNhap != null && x.Value.TenDangNhap.ToLower().Contains(lowerSearch)));
                    }

                    // Lọc theo vai trò (không phân biệt hoa thường)
                    if (!string.IsNullOrEmpty(roleFilter))
                    {
                        query = query.Where(x =>
                            x.Value.UserRole != null &&
                            x.Value.UserRole.Equals(roleFilter, StringComparison.OrdinalIgnoreCase));
                    }

                    foreach (var entry in query)
                    {
                        customerList.Add(new KeyValuePair<string, KhachHang>(entry.Key, entry.Value));
                    }
                }

                ViewBag.CurrentSearch = searchString;
                ViewBag.CurrentRole = roleFilter;

                return View(customerList);
            }
        }

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

        [HttpPost]
        public async Task<ActionResult> UpdateRole(string id, string newRole)
        {
            if (Session["UserRole"]?.ToString() != "Admin")
            {
                return new HttpUnauthorizedResult();
            }

            using (var client = new HttpClient())
            {
                // Get current user data
                var response = await client.GetAsync($"{FirebaseDbUrl}/users/{id}.json");
                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Không tìm thấy người dùng.";
                    return RedirectToAction("Index");
                }

                var json = await response.Content.ReadAsStringAsync();
                var user = JsonConvert.DeserializeObject<KhachHang>(json);

                // Update role
                user.UserRole = newRole;

                // Send update
                var updateResponse = await client.PutAsJsonAsync($"{FirebaseDbUrl}/users/{id}.json", user);
                if (updateResponse.IsSuccessStatusCode)
                {
                    TempData["Message"] = "Cập nhật vai trò thành công.";
                }
                else
                {
                    TempData["Error"] = "Không thể cập nhật vai trò.";
                }

                return RedirectToAction("Index");
            }
        }

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