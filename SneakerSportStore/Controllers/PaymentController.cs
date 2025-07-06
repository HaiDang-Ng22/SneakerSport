using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SneakerSportStore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SneakerSportStore.Controllers
{
    public class PaymentController : Controller
    {
        private readonly string FirebaseDbUrl = "https://sneakersportstore-default-rtdb.asia-southeast1.firebasedatabase.app/";

        // Lấy danh sách voucher mà user đã lưu trên Firebase
        private async Task<List<DiscountCode>> GetSavedVouchersOfUser(string userId)
        {
            var vouchers = new List<DiscountCode>();
            using (var client = new HttpClient())
            {
                // Lấy list voucherId từ node userVouchers
                var response = await client.GetAsync($"{FirebaseDbUrl}/userVouchers/{userId}.json");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var idList = JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
                    foreach (var voucherId in idList)
                    {
                        var voucherRes = await client.GetAsync($"{FirebaseDbUrl}/discountCodes/{voucherId}.json");
                        if (voucherRes.IsSuccessStatusCode)
                        {
                            var voucherJson = await voucherRes.Content.ReadAsStringAsync();
                            var voucher = JsonConvert.DeserializeObject<DiscountCode>(voucherJson);
                            if (voucher != null && voucher.Active)
                                vouchers.Add(voucher);
                        }
                    }
                }
            }
            return vouchers;
        }

        public async Task<ActionResult> Checkout()
        {
            var selectedCartItems = Session["SelectedCartItems"] as List<CartItem> ?? new List<CartItem>();  // Nếu null thì khởi tạo danh sách rỗng
            if (!selectedCartItems.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống. Vui lòng thêm sản phẩm để tiếp tục.";
                return RedirectToAction("Index", "Cart");
            }

            var userId = Session["CustomerID"]?.ToString();
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Lấy voucher đã lưu
            var savedVouchers = await GetSavedVouchersOfUser(userId);
            if (savedVouchers == null)
            {
                savedVouchers = new List<DiscountCode>(); // Nếu không có voucher, khởi tạo danh sách trống.
            }

            // Chuẩn bị ViewModel
            var vm = new CheckoutViewModel
            {
                SelectedCartItems = selectedCartItems,
                SavedVouchers = savedVouchers,
                TotalBeforeDiscount = selectedCartItems.Sum(x => (double)x.Price * x.Quantity),
                DiscountAmount = 0,
                TotalAfterDiscount = selectedCartItems.Sum(x => (double)x.Price * x.Quantity)
            };

            return View(vm);
        }

        //[HttpPost]
        //public async Task<ActionResult> Checkout(CheckoutViewModel model)
        //{
        //    var userId = Session["CustomerID"]?.ToString();
        //    var selectedCartItems = Session["SelectedCartItems"] as List<CartItem> ?? new List<CartItem>();

        //    // Tính tổng tiền trước khi áp dụng giảm giá
        //    var total = selectedCartItems.Sum(x => (double)x.Price * x.Quantity);
        //    model.TotalBeforeDiscount = total;

        //    // Đảm bảo SavedVouchers không null
        //    model.SavedVouchers = model.SavedVouchers ?? new List<DiscountCode>();

        //    // Lấy voucher đã chọn
        //    var voucher = model.SavedVouchers.FirstOrDefault(v => v.CodeName == model.SelectedVoucherId);

        //    // Kiểm tra điều kiện voucher: Tổng đơn hàng phải >= MinimumOrderValue của voucher
        //    if (!string.IsNullOrEmpty(model.SelectedVoucherId) && (voucher == null || total < voucher.MinimumOrderValue))
        //    {
        //        // Thêm thông báo lỗi nếu không đủ điều kiện
        //        ModelState.AddModelError(nameof(model.SelectedVoucherId),
        //            $"Voucher không đủ điều kiện (Đơn hàng phải lớn hơn hoặc bằng {voucher?.MinimumOrderValue.ToString("N0")} VNĐ).");
        //        model.TotalAfterDiscount = total;
        //        return View(model); // Trả về View với thông báo lỗi
        //    }

        //    // Xử lý giảm giá nếu voucher hợp lệ
        //    model.DiscountAmount = 0;
        //    if (voucher != null)
        //    {
        //        if (voucher.DiscountType == "Percentage") // Giảm giá theo phần trăm
        //        {
        //            model.DiscountAmount = total * voucher.DiscountValue / 100.0;
        //        }
        //        else if (voucher.DiscountType == "Fixed") // Giảm giá cố định
        //        {
        //            model.DiscountAmount = voucher.DiscountValue;
        //        }
        //    }

        //    // Tính tổng tiền sau khi áp dụng giảm giá
        //    model.TotalAfterDiscount = total - model.DiscountAmount;

        //    // Tạo đơn hàng và lưu lên Firebase
        //    var order = new Order
        //    {
        //        OrderId = Guid.NewGuid().ToString(),
        //        UserId = userId,
        //        CustomerName = model.Name,
        //        PhoneNumber = model.Phone,
        //        Address = model.Address,
        //        OrderDate = DateTime.Now,
        //        Items = selectedCartItems.Select(x => new OrderItem
        //        {
        //            ProductId = x.ProductId,
        //            ProductName = x.ProductName,
        //            Quantity = x.Quantity,
        //            Price = x.Price
        //        }).ToList(),
        //        Total = total,
        //        DiscountCode = voucher?.CodeName ?? "",
        //        DiscountAmount = model.DiscountAmount,
        //        FinalTotal = model.TotalAfterDiscount,
        //        PaymentMethod = model.PaymentMethod
        //    };

        //    // Lưu đơn hàng vào Firebase và trừ kho
        //    using (var client = new HttpClient())
        //    {
        //        var json = JsonConvert.SerializeObject(order);
        //        var content = new StringContent(json, Encoding.UTF8, "application/json");
        //        await client.PutAsync($"{FirebaseDbUrl}/orders/{order.OrderId}.json", content);

        //        // Trừ kho
        //        foreach (var it in order.Items)
        //            await ReduceProductStock(it.ProductId, it.Quantity);
        //    }

        //    // Xóa giỏ hàng và thông báo thành công
        //    Session["SelectedCartItems"] = null;
        //    TempData["SuccessMessage"] = "Đặt hàng thành công!";
        //    return RedirectToAction("OrderResult", new { id = order.OrderId });
        //}

        // Phương thức checkout để xử lý đơn hàng
        [HttpPost]
        public async Task<ActionResult> Checkout(CheckoutViewModel model)
        {
            var selectedCartItems = Session["SelectedCartItems"] as List<CartItem> ?? new List<CartItem>();
            var total = selectedCartItems.Sum(x => (double)x.Price * x.Quantity);
            model.TotalBeforeDiscount = total;

            // Đảm bảo SavedVouchers không null
            model.SavedVouchers = model.SavedVouchers ?? new List<DiscountCode>();

            // Lấy voucher đã chọn
            var voucher = model.SavedVouchers.FirstOrDefault(v => v.CodeName == model.SelectedVoucherId);

            // Kiểm tra điều kiện voucher: Tổng đơn hàng phải >= MinimumOrderValue của voucher
            if (!string.IsNullOrEmpty(model.SelectedVoucherId) && (voucher == null || total < voucher.MinimumOrderValue))
            {
                ModelState.AddModelError(nameof(model.SelectedVoucherId),
                    $"Voucher không đủ điều kiện (Đơn hàng phải lớn hơn hoặc bằng {voucher?.MinimumOrderValue.ToString("N0")} VNĐ).");
                model.TotalAfterDiscount = total;
                return View(model); // Trả về View với thông báo lỗi
            }

            // Xử lý giảm giá nếu voucher hợp lệ
            model.DiscountAmount = 0;
            if (voucher != null)
            {
                if (voucher.DiscountType == "Percentage") // Giảm giá theo phần trăm
                {
                    model.DiscountAmount = total * voucher.DiscountValue / 100.0;
                }
                else if (voucher.DiscountType == "Fixed") // Giảm giá cố định
                {
                    model.DiscountAmount = voucher.DiscountValue;
                }
            }

            model.TotalAfterDiscount = total - model.DiscountAmount;

            // Tạo đơn hàng và lưu lên Firebase
            var order = new Order
            {
                OrderId = Guid.NewGuid().ToString(),
                UserId = Session["CustomerID"].ToString(),
                CustomerName = model.Name,
                PhoneNumber = model.Phone,
                Address = model.Address,
                OrderDate = DateTime.Now,
                Items = selectedCartItems.Select(x => new OrderItem
                {
                    ProductId = x.ProductId,
                    ProductName = x.ProductName,
                    Quantity = x.Quantity,
                    Price = x.Price
                }).ToList(),
                Total = total,
                DiscountCode = voucher?.CodeName ?? "",
                DiscountAmount = model.DiscountAmount,
                FinalTotal = model.TotalAfterDiscount,
                PaymentMethod = model.PaymentMethod
            };

            // Lưu đơn hàng vào Firebase
            using (var client = new HttpClient())
            {
                var json = JsonConvert.SerializeObject(order);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PutAsync($"{FirebaseDbUrl}/orders/{order.OrderId}.json", content);

                if (response.IsSuccessStatusCode)
                {
                    // Trừ kho sản phẩm sau khi đơn hàng được tạo thành công
                    foreach (var it in order.Items)
                        await ReduceProductStock(it.ProductId, it.Quantity);

                    // Gửi thông báo cho admin sau khi đơn hàng được lưu
                    await SendOrderNotification(order.OrderId, order.UserId);
                }
                else
                {
                    // Nếu lưu đơn hàng không thành công, log thông báo lỗi
                    Console.WriteLine("Lỗi khi lưu đơn hàng vào Firebase: " + response.StatusCode);
                }
            }

            // Xóa giỏ hàng và thông báo thành công
            Session["SelectedCartItems"] = null;
            TempData["SuccessMessage"] = "Đặt hàng thành công!";
            return RedirectToAction("OrderResult", new { id = order.OrderId });
        }
        private async Task SendOrderNotification(string orderId, string userId)
        {
            // Create the admin notification
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


        public ActionResult OrderResult(string id)
        {
            ViewBag.OrderId = id;
            return View();
        }

        // LẤY TỒN KHO SẢN PHẨM
        private async Task<int> GetProductStock(string productId)
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(FirebaseDbUrl + $"/products/{productId}.json");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var product = JsonConvert.DeserializeObject<Giay>(json);
                    return product?.SoLuongTon ?? 0;
                }
            }
            return 0;
        }

        // TRỪ KHO
        private async Task ReduceProductStock(string productId, int quantity)
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(FirebaseDbUrl + $"/products/{productId}.json");
                if (!response.IsSuccessStatusCode) return;
                var json = await response.Content.ReadAsStringAsync();
                var product = JsonConvert.DeserializeObject<Giay>(json);
                if (product == null) return;

                product.SoLuongTon = Math.Max(0, product.SoLuongTon - quantity);

                var content = new StringContent(JsonConvert.SerializeObject(product), System.Text.Encoding.UTF8, "application/json");
                await client.PutAsync(FirebaseDbUrl + $"/products/{productId}.json", content);
            }
        }
        //private async Task SendOrderNotification(string orderId, string userId)
        //{
        //    // Tạo thông báo cho Admin
        //    var adminNotification = new Notification
        //    {
        //        Message = $"Đơn hàng mới {orderId} của người dùng {userId} đang chờ xác nhận.",
        //        IsRead = false,
        //        CreatedAt = DateTime.Now,
        //        Type = "OrderUpdate", // Type of notification
        //        RelatedOrderId = orderId,
        //        RedirectUrl = Url.Action("Details", "AdminOrder", new { id = userId }, protocol: Request.Url.Scheme) // URL to view the order details for admin
        //    };

        //    // Tạo thông báo cho User
        //    var userNotification = new Notification
        //    {
        //        Message = $"Đơn hàng của bạn ({orderId}) đã được đặt thành công!",
        //        IsRead = false,
        //        CreatedAt = DateTime.Now,
        //        Type = "OrderSuccess",
        //        RelatedOrderId = orderId,
        //        RedirectUrl = Url.Action("OrderDetails", "UserOrder", new { orderId }, protocol: Request.Url.Scheme) // URL to view the order details for user
        //    };

        //    using (var client = new HttpClient())
        //    {
        //        // Lưu thông báo cho Admin
        //        var jsonAdmin = JsonConvert.SerializeObject(adminNotification);
        //        var contentAdmin = new StringContent(jsonAdmin, Encoding.UTF8, "application/json");
        //        await client.PostAsync(FirebaseDbUrl + "/notifications/admin.json", contentAdmin);

        //        // Lưu thông báo cho User
        //        var jsonUser = JsonConvert.SerializeObject(userNotification);
        //        var contentUser = new StringContent(jsonUser, Encoding.UTF8, "application/json");
        //        await client.PostAsync(FirebaseDbUrl + $"/notifications/{userId}.json", contentUser);
        //    }
        //}



    }


    public class OrderItem
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
