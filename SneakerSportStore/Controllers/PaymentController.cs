using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QRCoder;
using SneakerSportStore.Models;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Firebase.Storage; // Thêm dòng này
using System.Net.Http.Headers;
using System.Threading;

namespace SneakerSportStore.Controllers
{
    public class PaymentController : Controller
    {
        private const string FirebaseDbUrl = "https://sneakersportstore-default-rtdb.asia-southeast1.firebasedatabase.app/";
        private const string AppliedVoucherSessionKey = "AppliedVoucher";
        private const string FirebaseStorageBucket = "sneakersportstore.appspot.com"; 

        // Static HttpClient for efficient connection reuse
        private static readonly HttpClient httpClient = new HttpClient();

        // GET: Checkout
        public async Task<ActionResult> Checkout()
        {
            var selectedCartItems = Session["SelectedCartItems"] as List<CartItem> ?? new List<CartItem>();
            if (!selectedCartItems.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống. Vui lòng thêm sản phẩm để tiếp tục.";
                return RedirectToAction("Index", "Cart");
            }

            var userId = Session["UserId"]?.ToString();
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var savedVouchers = await GetSavedVouchersOfUser(userId) ?? new List<DiscountCode>();
            var total = selectedCartItems.Sum(x => (double)x.Price * x.Quantity);

            var appliedVoucher = Session[AppliedVoucherSessionKey] as DiscountCode;
            double discount = 0;

            if (appliedVoucher != null)
            {
                discount = CalculateDiscount(total, appliedVoucher);
            }

            var vm = new CheckoutViewModel
            {
                SelectedCartItems = selectedCartItems,
                SavedVouchers = savedVouchers,
                TotalBeforeDiscount = total,
                TotalAfterDiscount = total - discount,
                AppliedVoucher = appliedVoucher,
                DiscountAmount = discount
            };

            return View(vm);
        }


        private async Task<List<DiscountCode>> GetSavedVouchersOfUser(string userId)
        {
            try
            {
                var response = await httpClient.GetAsync($"{FirebaseDbUrl}/userVouchers/{userId}.json");
                if (!response.IsSuccessStatusCode) return new List<DiscountCode>();

                var json = await response.Content.ReadAsStringAsync();
                var idList = JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();

                var tasks = idList.Select(GetVoucherDetails);
                var results = await Task.WhenAll(tasks);

                return results.Where(v => v != null && v.Active).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting vouchers: {ex.Message}");
                return new List<DiscountCode>();
            }
        }


        private async Task<DiscountCode> GetVoucherDetails(string voucherId)
        {
            try
            {
                var response = await httpClient.GetAsync($"{FirebaseDbUrl}/discountCodes/{voucherId}.json");
                if (!response.IsSuccessStatusCode) return null;

                var voucherJson = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<DiscountCode>(voucherJson);
            }
            catch
            {
                return null;
            }
        }

        private double CalculateDiscount(double total, DiscountCode voucher)
        {
            if (voucher == null) return 0;
            if ((double)voucher.MinimumOrderValue > total) return 0;

            return voucher.DiscountType == DiscountType.Percentage
                ? total * (double)voucher.DiscountValue / 100
                : (double)voucher.DiscountValue;
        }

        [HttpPost]
        public async Task<ActionResult> ApplyVoucher(string voucherCode)
        {
            var userId = Session["UserId"]?.ToString();
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            var savedVouchers = await GetSavedVouchersOfUser(userId);
            var voucher = savedVouchers.FirstOrDefault(v => v.CodeName == voucherCode);

            if (voucher == null)
            {
                return Json(new { success = false, message = "Mã voucher không hợp lệ" });
            }

            Session[AppliedVoucherSessionKey] = voucher;

            return Json(new { success = true });
        }

        [HttpPost]
        public ActionResult RemoveVoucher()
        {
            Session.Remove(AppliedVoucherSessionKey);
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<ActionResult> Checkout(CheckoutViewModel model)
        {
            System.Diagnostics.Debug.WriteLine($"File uploaded: {model.BankTransferImage?.FileName}");
            string screenshotUrl = null; 

            var selectedCartItems = Session["SelectedCartItems"] as List<CartItem> ?? new List<CartItem>();
            if (!selectedCartItems.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index", "Cart");
            }
            if (model.BankTransferImage != null && model.BankTransferImage.ContentLength > 0)
            {
                screenshotUrl = await UploadImageToFirebaseStorage(model.BankTransferImage);
            }
            double total = selectedCartItems.Sum(x => (double)x.Price * x.Quantity);
            model.TotalBeforeDiscount = total;

            var appliedVoucher = Session[AppliedVoucherSessionKey] as DiscountCode;
            double discount = 0;

            if (appliedVoucher != null)
            {
                discount = CalculateDiscount(total, appliedVoucher);
                model.DiscountAmount = discount;
                model.TotalAfterDiscount = total - discount;
            }
            else
            {
                model.TotalAfterDiscount = total;
            }
            if (model.BankTransferImage != null && model.BankTransferImage.ContentLength > 0)
            {
                using (var ms = new MemoryStream())
                {
                    model.BankTransferImage.InputStream.CopyTo(ms);
                    var imageBytes = ms.ToArray();
                    model.BankTransferScreenshot = $"data:{model.BankTransferImage.ContentType};base64," + Convert.ToBase64String(imageBytes);
                }
            }
            var order = new Order
            {
                OrderId = Guid.NewGuid().ToString(),
                UserId = Session["UserId"]?.ToString(),
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
                Total = total,// tổng tiền
                DiscountCode = appliedVoucher?.CodeName ?? "",// vô chờ
                DiscountAmount = discount,// số tiền giảm giá
                FinalTotal = model.TotalAfterDiscount,// Tổng Tiền cuối c
                PaymentMethod = model.PaymentMethod,
                BankTransferScreenshot = screenshotUrl
            };

            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(order), Encoding.UTF8, "application/json");
                var response = await httpClient.PutAsync($"{FirebaseDbUrl}/orders/{order.OrderId}.json", content);

                if (response.IsSuccessStatusCode)
                {
                    await ReduceProductStock(selectedCartItems);

                    if (model.PaymentMethod == "bank")
                    {
                        var bankDetails = new BankTransferDetails
                        {
                            BankName = "Techcombank",
                            AccountNumber = "0799192226",
                            AccountHolder = "Công ty SneakerSportStore",
                            Amount = model.TotalAfterDiscount,
                            OrderId = order.OrderId
                        };
                        model.QRCodeBase64 = GenerateQRCode(bankDetails);
                    }

                    Session["SelectedCartItems"] = null;
                    Session.Remove(AppliedVoucherSessionKey);

                    await SendOrderNotification(order.OrderId, order.UserId);

                    TempData["SuccessMessage"] = "Đặt hàng thành công!";
                    return RedirectToAction("OrderResult", new { id = order.OrderId });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Checkout error: {ex.Message}");
            }

            TempData["ErrorMessage"] = "Có lỗi xảy ra khi đặt hàng. Vui lòng thử lại.";
            return View(model);
        }
        private async Task<string> UploadImageToFirebaseStorage(HttpPostedFileBase file)
        {
            try
            {
                var stream = file.InputStream;
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

                var task = new FirebaseStorage(
                    FirebaseStorageBucket,
                    new FirebaseStorageOptions
                    {
                        AuthTokenAsyncFactory = () => Task.FromResult("") // Bỏ trống nếu không dùng xác thực
                    })
                    .Child("payment-proofs")
                    .Child(fileName)
                    .PutAsync(stream, new CancellationToken(), file.ContentType);

                return await task;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Image upload failed: {ex.Message}");
                return null;
            }
        }

        private async Task ReduceProductStock(List<CartItem> cartItems)
        {
            try
            {
                var tasks = cartItems.Select(async item =>
                {
                    var response = await httpClient.GetAsync($"{FirebaseDbUrl}/products/{item.ProductId}.json");
                    if (!response.IsSuccessStatusCode) return;

                    var json = await response.Content.ReadAsStringAsync();
                    var product = JsonConvert.DeserializeObject<ProductFireBaseKey>(json);
                    if (product == null) return;

                    product.SoLuongTon = Math.Max(0, product.SoLuongTon - item.Quantity);

                    var content = new StringContent(JsonConvert.SerializeObject(product), Encoding.UTF8, "application/json");
                    await httpClient.PutAsync($"{FirebaseDbUrl}/products/{item.ProductId}.json", content);
                });

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Reduce stock error: {ex.Message}");
            }
        }

        private string GenerateQRCode(BankTransferDetails bankDetails)
        {
            string paymentDetails = $"Bank: {bankDetails.BankName}\n" +
                                    $"Account Number: {bankDetails.AccountNumber}\n" +
                                    $"Account Holder: {bankDetails.AccountHolder}\n" +
                                    $"Amount: {bankDetails.Amount} VND\n" +
                                    $"Order ID: {bankDetails.OrderId}";

            using (var qrGenerator = new QRCodeGenerator())
            {
                var qrCodeData = qrGenerator.CreateQrCode(paymentDetails, QRCodeGenerator.ECCLevel.Q);
                using (var qrCode = new QRCode(qrCodeData))
                using (var stream = new MemoryStream())
                {
                    qrCode.GetGraphic(20).Save(stream, ImageFormat.Png);
                    return Convert.ToBase64String(stream.ToArray());
                }
            }
        }

        //private async Task SendOrderNotification(string orderId, string userId)
        //{
        //    try
        //    {
        //        var adminUserIds = await GetAdminUserIds();
        //        if (!adminUserIds.Any()) return;

        //        var notification = new Notification
        //        {
        //            Message = $"Đơn hàng mới #{orderId} đang chờ xử lý",
        //            IsRead = false,
        //            CreatedAt = DateTime.Now,
        //            RelatedOrderId = orderId,
        //            RedirectUrl = Url.Action("Details", "AdminOrder", new { id = orderId }, Request.Url?.Scheme)
        //        };

        //        var tasks = adminUserIds.Select(async adminId =>
        //        {
        //            var content = new StringContent(JsonConvert.SerializeObject(notification), Encoding.UTF8, "application/json");
        //            await httpClient.PostAsync($"{FirebaseDbUrl}/notifications/{adminId}.json", content);
        //        });

        //        await Task.WhenAll(tasks);
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Diagnostics.Debug.WriteLine($"Notification error: {ex.Message}");
        //    }
        //}

        //private async Task<List<string>> GetAdminUserIds()
        //{
        //    var adminIds = new List<string>();
        //    try
        //    {
        //        var response = await httpClient.GetAsync($"{FirebaseDbUrl}/users.json");
        //        if (!response.IsSuccessStatusCode) return adminIds;

        //        var json = await response.Content.ReadAsStringAsync();
        //        var users = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(json);

        //        if (users != null)
        //        {
        //            adminIds = users
        //                .Where(u => u.Value["UserRole"]?.ToString() == "Admin")
        //                .Select(u => u.Key)
        //                .ToList();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // Log error
        //        System.Diagnostics.Debug.WriteLine($"Get admin IDs error: {ex.Message}");
        //    }
        //    return adminIds;
        //}

        private async Task SendOrderNotification(string orderId, string userId)
        {
            var adminNotification = new Notification
            {
                Message = $"Đơn hàng mới {orderId} của người dùng {userId} đang chờ xác nhận.",
                IsRead = false,
                CreatedAt = DateTime.Now,
                Type = "OrderUpdate",
                RelatedOrderId = orderId,
                RedirectUrl = Url.Action("Details", "AdminOrder", new { id = orderId }, protocol: Request.Url.Scheme)
            };

            var adminUserIds = await GetAdminUserIds();

            using (var client = new HttpClient())
            {
                foreach (var adminId in adminUserIds)
                {
                    var jsonAdmin = JsonConvert.SerializeObject(adminNotification);
                    var contentAdmin = new StringContent(jsonAdmin, Encoding.UTF8, "application/json");

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

                    Console.WriteLine("Received JSON: " + json);

                    var dict = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(json);

                    if (dict != null)
                    {
                        foreach (var pair in dict)
                        {
                            var user = pair.Value as JObject; 
                            if (user != null && user["userRole"]?.ToString() == "Admin")
                            {
                                adminUserIds.Add(pair.Key); 
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
    }
}