using SneakerSportStore.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace SneakerSportStore.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly string FirebaseDbUrl = "https://sneakersportstore-default-rtdb.asia-southeast1.firebasedatabase.app";

        // GET: Checkout - Hiển thị trang thanh toán
        public async Task<ActionResult> Index(string productId, int? quantity)
        {
            quantity = quantity ?? 1;

            // Kiểm tra xem người dùng đã đăng nhập chưa
            if (Session["CustomerID"] == null)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để thanh toán.";
                return RedirectToAction("Login", "Account");
            }

            var product = await GetProductById(productId);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Sản phẩm không tồn tại.";
                return RedirectToAction("Index", "Home");
            }

            // Tính tổng số tiền
            var totalAmount = product.Gia * quantity.Value;

            // Tạo model cho trang Checkout
            var model = new CheckoutViewModel
            {
                ProductId = productId,
                ProductName = product.TenGiay,
                Price = product.Gia,
                Quantity = quantity.Value,
                TotalAmount = totalAmount,
                PaymentMethods = new List<SelectListItem>
                {
                    new SelectListItem { Text = "Chuyển khoản", Value = "Transfer" },
                    new SelectListItem { Text = "Thanh toán khi nhận hàng", Value = "COD" },
                    new SelectListItem { Text = "Thẻ tín dụng / Ghi nợ", Value = "CreditDebit" },
                    new SelectListItem { Text = "Momo", Value = "Momo" }
                }
            };

            return View(model);
        }

        // Lấy sản phẩm từ Firebase theo productId
        private async Task<Giay> GetProductById(string productId)
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(FirebaseDbUrl + $"/products/{productId}.json");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<Giay>(json);
                }
                return null;
            }
        }

        // POST: Xác nhận đơn hàng - xử lý đơn hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Confirm(CheckoutViewModel model)
        {
            if (ModelState.IsValid)
            {
                var customerId = Session["CustomerID"]?.ToString();
                if (string.IsNullOrEmpty(customerId))
                {
                    TempData["ErrorMessage"] = "Bạn phải đăng nhập trước khi đặt hàng.";
                    return RedirectToAction("Login", "Account");
                }

                var order = new Order
                {
                    UserId = customerId,
                    ProductId = model.ProductId,
                    ProductName = model.ProductName,
                    Quantity = model.Quantity,
                    Price = model.Price,
                    TotalAmount = model.TotalAmount,
                    PaymentMethod = model.PaymentMethod,
                    ShippingAddress = model.Address,
                    Phone = model.Phone,
                    Email = model.Email,
                    OrderDate = DateTime.Now,
                    Status = "Processing"
                };

                using (var client = new HttpClient())
                {
                    var jsonContent = JsonConvert.SerializeObject(order);
                    var response = await client.PostAsync(FirebaseDbUrl + "/orders.json", new StringContent(jsonContent, Encoding.UTF8, "application/json"));

                    if (response.IsSuccessStatusCode)
                    {
                        // Sau khi đặt hàng thành công, xóa giỏ hàng của người dùng
                        await ClearCartFromFirebase(customerId);

                        TempData["SuccessMessage"] = "Đơn hàng của bạn đã được đặt thành công!";
                        return RedirectToAction("OrderConfirmation");
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Có lỗi xảy ra khi lưu đơn hàng.";
                        return View("Index", model);
                    }
                }
            }

            TempData["ErrorMessage"] = "Đã xảy ra lỗi trong quá trình đặt hàng.";
            return View("Index", model);
        }

        // Xóa giỏ hàng khỏi Firebase sau khi thanh toán thành công
        private async Task ClearCartFromFirebase(string userId)
        {
            using (var client = new HttpClient())
            {
                var response = await client.DeleteAsync(FirebaseDbUrl + $"/carts/{userId}.json");
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Không thể xóa giỏ hàng khỏi Firebase");
                }
            }
        }

        // Trang xác nhận đơn hàng
        public ActionResult OrderConfirmation()
        {
            return View();
        }
    }
}
