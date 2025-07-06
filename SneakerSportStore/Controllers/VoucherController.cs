using Newtonsoft.Json;
using SneakerSportStore.Extensions;
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
    public class VoucherController : Controller
    {
        private readonly string FirebaseDbUrl = "https://sneakersportstore-default-rtdb.asia-southeast1.firebasedatabase.app/";

        // Show available public vouchers
        public async Task<ActionResult> PromotionNews()
        {
            var vouchers = await GetPublicVouchers();
            return View(vouchers);
        }

        private async Task<List<DiscountCode>> GetPublicVouchers()
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(FirebaseDbUrl + "/discountCodes.json");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    // Deserialize thành Dictionary<string, object> để kiểm tra kiểu value
                    var rawDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                    var result = new List<DiscountCode>();

                    if (rawDict != null)
                    {
                        foreach (var kv in rawDict)
                        {
                            // Chỉ xử lý nếu value là JObject (tức là object mã giảm giá)
                            if (kv.Value is Newtonsoft.Json.Linq.JObject jobj)
                            {
                                var code = jobj.ToObject<DiscountCode>();
                                if (code != null && code.IsPublic && code.Active)
                                {
                                    result.Add(code);
                                }
                            }
                        }
                    }
                    return result;
                }
                return new List<DiscountCode>();
            }
        }


        [HttpPost]
        public async Task<ActionResult> SaveDiscountCode(string voucherId)
        {
            var userId = Session["CustomerID"]?.ToString();
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(FirebaseDbUrl + $"/userVouchers/{userId}.json");
                var codes = new List<string>();
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(json) && json != "null")
                        codes = JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
                }
                if (!codes.Contains(voucherId))
                {
                    codes.Add(voucherId);
                    var content = new StringContent(JsonConvert.SerializeObject(codes), Encoding.UTF8, "application/json");
                    await client.PutAsync(FirebaseDbUrl + $"/userVouchers/{userId}.json", content);
                    TempData["SuccessMessage"] = "Mã giảm giá đã được lưu thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Mã giảm giá đã được lưu trước đó!";
                }
            }
            return RedirectToAction("PromotionNews");
        }



        // Danh sách voucher user đã lưu
        public async Task<ActionResult> UserVouchers()
        {
            var userId = Session["CustomerID"]?.ToString();
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var vouchers = await GetSavedVouchersForUser(userId);
            return View(vouchers);
        }

        // Lấy list voucher (DiscountCode) theo userId
        private async Task<List<DiscountCode>> GetSavedVouchersForUser(string userId)
        {
            var vouchers = new List<DiscountCode>();
            using (var client = new HttpClient())
            {
                var res = await client.GetAsync($"{FirebaseDbUrl}/userVouchers/{userId}.json");
                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadAsStringAsync();
                    var codes = JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
                    foreach (var idVoucher in codes)
                    {
                        var voucherRes = await client.GetAsync($"{FirebaseDbUrl}/discountCodes/{idVoucher}.json");
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

        // Xóa voucher khỏi user (ví dụ dùng khi dùng xong hoặc bấm xóa voucher)
        [HttpPost]
        public async Task<ActionResult> RemoveVoucher(string idVoucher)
        {
            var userId = Session["CustomerID"]?.ToString();
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            using (var client = new HttpClient())
            {
                var res = await client.GetAsync($"{FirebaseDbUrl}/userVouchers/{userId}.json");
                var codes = new List<string>();
                if (res.IsSuccessStatusCode)
                {
                    var json = await res.Content.ReadAsStringAsync();
                    codes = JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
                }
                if (codes.Contains(idVoucher))
                {
                    codes.Remove(idVoucher);
                    var content = new StringContent(JsonConvert.SerializeObject(codes), Encoding.UTF8, "application/json");
                    await client.PutAsync($"{FirebaseDbUrl}/userVouchers/{userId}.json", content);
                }
            }
            return RedirectToAction("UserVouchers");
        }

            // Tạo mã giảm giá mới (admin)
            public ActionResult CreateDiscountCode()
            {
                if (Session["UserRole"] == null || Session["UserRole"].ToString() != "Admin")
                    return RedirectToAction("Login", "Account");
                return View();
            }

            [HttpPost]
            public async Task<ActionResult> CreateDiscountCode(string codeName, string condition, string discountType, double discountValue, DateTime startDate, DateTime endDate, bool isPublic, double minimumOrderValue)
            {
                if (Session["UserRole"] == null || Session["UserRole"].ToString() != "Admin")
                    return RedirectToAction("Login", "Account");

                string idVoucher = Guid.NewGuid().ToString();
                var discountData = new
                {
                    IdVoucher = idVoucher,
                    CodeName = codeName,
                    Condition = condition,
                    DiscountType = discountType,
                    DiscountValue = discountValue,
                    StartDate = startDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                    EndDate = endDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                    Active = true,
                    IsPublic = isPublic,
                    MinimumOrderValue = minimumOrderValue
                };

                using (var client = new HttpClient())
                {
                    var content = new StringContent(JsonConvert.SerializeObject(discountData), Encoding.UTF8, "application/json");
                    var response = await client.PutAsync($"{FirebaseDbUrl}/discountCodes/{idVoucher}.json", content);

                    if (response.IsSuccessStatusCode)
                    {
                        TempData["SuccessMessage"] = "Mã giảm giá đã được tạo thành công!";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Lỗi khi tạo mã giảm giá.";
                    }
                }
                return RedirectToAction("CreateDiscountCode");
            }

        // Quản lý mã giảm giá (admin)
        public async Task<ActionResult> ManageDiscountCodes()
        {
            if (Session["UserRole"] == null || Session["UserRole"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");

            var discountCodes = await GetDiscountCodes();
            if (discountCodes == null || !discountCodes.Any())
            {
                TempData["ErrorMessage"] = "Không có mã giảm giá nào để hiển thị.";
                return View(new List<DiscountCode>());
            }
            return View(discountCodes);
        }
        private async Task<List<DiscountCode>> GetDiscountCodes()
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(FirebaseDbUrl + "/discountCodes.json");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    // Parse dạng Dictionary<string, object> trước
                    var rawDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                    var result = new List<DiscountCode>();

                    if (rawDict != null)
                    {
                        foreach (var kv in rawDict)
                        {
                            // Nếu value là dạng object (voucher), thì convert sang DiscountCode, còn nếu là true/false thì bỏ qua
                            if (kv.Value is Newtonsoft.Json.Linq.JObject jobj)
                            {
                                var code = jobj.ToObject<DiscountCode>();
                                if (code != null) result.Add(code);
                            }
                        }
                    }
                    return result;
                }
                return new List<DiscountCode>();
            }
        }


        // Ẩn/hiện mã giảm giá (admin)
        [HttpPost]
        public async Task<ActionResult> ToggleDiscountCode(string idVoucher, bool isActive)
        {
            var updateData = new { Active = isActive };
            using (var client = new HttpClient())
            {
                var content = new StringContent(JsonConvert.SerializeObject(updateData), Encoding.UTF8, "application/json");
                var response = await client.PatchAsync($"{FirebaseDbUrl}/discountCodes/{idVoucher}.json", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Cập nhật trạng thái mã giảm giá thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể cập nhật trạng thái mã giảm giá.";
                }
            }
            return RedirectToAction("ManageDiscountCodes");
        }
    }
}
