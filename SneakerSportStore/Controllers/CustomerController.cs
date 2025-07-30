using SneakerSportStore.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using System;
using Newtonsoft.Json.Linq;
using System.Text;
using SneakerSportStore.Extensions;

namespace SneakerSportStore.Controllers
{
    public class CustomerController : Controller
    {
        private readonly string FirebaseDbUrl = "https://sneakersportstore-default-rtdb.asia-southeast1.firebasedatabase.app/";

        public async Task<ActionResult> Index(string searchString)
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync($"{FirebaseDbUrl}/users.json");
                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Không thể tải dữ liệu khách hàng.";
                    return View(new List<KeyValuePair<string, UserInfo>>());
                }

                var json = await response.Content.ReadAsStringAsync();
                var dict = JsonConvert.DeserializeObject<Dictionary<string, UserInfo>>(json);
                var customerList = new List<KeyValuePair<string, UserInfo>>();

                if (dict != null)
                {
                    // Lọc chỉ các tài khoản có UserRole = "User"
                    var query = dict.Where(x =>
                        !string.IsNullOrEmpty(x.Value.UserRole) &&
                        x.Value.UserRole.Equals("User", StringComparison.OrdinalIgnoreCase));

                    // Tìm kiếm theo tên, email, hoặc username
                    if (!string.IsNullOrEmpty(searchString))
                    {
                        var lowerSearch = searchString.ToLower();
                        query = query.Where(x =>
                            (x.Value.HoTen != null && x.Value.HoTen.ToLower().Contains(lowerSearch)) ||
                            (x.Value.Email != null && x.Value.Email.ToLower().Contains(lowerSearch)) ||
                            (x.Value.TenDangNhap != null && x.Value.TenDangNhap.ToLower().Contains(lowerSearch)));
                    }

                    customerList = query.ToList();
                }

                ViewBag.CurrentSearch = searchString;
                ViewBag.HasSearch = !string.IsNullOrEmpty(searchString);
                return View(customerList);
            }
        }
        [HttpGet]
        public async Task<ActionResult> Staff(string roleFilter)
        {
            // Chỉ Admin mới được truy cập
            if (Session["UserRole"]?.ToString() != "Admin")
            {
                TempData["Error"] = "Bạn không có quyền truy cập trang này";
                return RedirectToAction("Index", "Home");
            }

            // Danh sách các bộ phận
            var categories = new List<SelectListItem>
            {
                new SelectListItem { Text = "Nhân viên", Value = "nhanvien" },
                new SelectListItem { Text = "Giao hàng", Value = "giaohang" },
                new SelectListItem { Text = "CSKH", Value = "chamsockhachhang" },
                new SelectListItem { Text = "Kho", Value = "kho" },
                new SelectListItem { Text = "Kế toán", Value = "ketoan" }
            };
            ViewBag.Categories = categories;
            ViewBag.SelectedRole = roleFilter;

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync($"{FirebaseDbUrl}/users.json");
                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Không thể tải dữ liệu nhân viên.";
                    return View(new List<KeyValuePair<string, UserInfo>>());
                }

                var json = await response.Content.ReadAsStringAsync();
                var dict = JsonConvert.DeserializeObject<Dictionary<string, UserInfo>>(json);
                var staffList = new List<KeyValuePair<string, UserInfo>>();

                if (dict != null && !string.IsNullOrEmpty(roleFilter))
                {
                    staffList = dict
                        .Where(x => !string.IsNullOrEmpty(x.Value.UserRole) &&
                            x.Value.UserRole.Equals(roleFilter, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                return View(staffList);
            }
        }


        public async Task<ActionResult> Details(string id)
        {
            // Kiểm tra quyền Admin
            if (Session["UserRole"]?.ToString() != "Admin")
            {
                TempData["Error"] = "Bạn không có quyền truy cập trang này";
                return RedirectToAction("Index", "Home");
            }

            using (var client = new HttpClient())
            {
                var userResponse = await client.GetAsync($"{FirebaseDbUrl}/users/{id}.json");
                if (!userResponse.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Không tìm thấy người dùng.";
                    return RedirectToAction("Index");
                }

                var userJson = await userResponse.Content.ReadAsStringAsync();
                var user = JsonConvert.DeserializeObject<UserInfo>(userJson);

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
                            if (item.Value.UserId == id)
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
                // Chuẩn hóa tên thuộc tính
                var patchData = new { userRole = newRole };
                var content = new StringContent(
                    JsonConvert.SerializeObject(patchData),
                    Encoding.UTF8,
                    "application/json");

                var response = await client.PatchAsync($"{FirebaseDbUrl}/users/{id}.json", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Message"] = "Cập nhật vai trò thành công.";
                }
                else
                {
                    TempData["Error"] = $"Lỗi cập nhật: {response.StatusCode}";
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
                    TempData["Error"] = $"Lỗi khi xóa: {response.StatusCode}";
                }
                return RedirectToAction("Index");
            }
        }
    }
}