using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SneakerSportStore.Extensions;
using SneakerSportStore.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        private const int MaxVouchersPerUser = 10; // Giới hạn voucher mỗi người dùng
        private const int MaxVoucherSaveAttempts = 3; // Số lần thử lại khi save thất bại

        // Cache dữ liệu voucher public để tăng hiệu suất
        private static List<DiscountCode> _publicVouchersCache;
        private static DateTime _cacheExpiration = DateTime.MinValue;

        public async Task<ActionResult> PromotionNews()
        {
            var userId = Session["UserId"]?.ToString();
            var savedVoucherIds = new List<string>();

            if (!string.IsNullOrEmpty(userId))
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(FirebaseDbUrl + $"/userVouchers/{userId}.json");
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        savedVoucherIds = string.IsNullOrEmpty(json) || json == "null"
                            ? new List<string>()
                            : JsonConvert.DeserializeObject<List<string>>(json);
                    }
                }
            }

            ViewBag.SavedVouchers = savedVoucherIds;

            if (_publicVouchersCache == null || DateTime.Now > _cacheExpiration)
            {
                _publicVouchersCache = await GetPublicVouchers();
                _cacheExpiration = DateTime.Now.AddMinutes(5);
            }

            return View(_publicVouchersCache);
        }

        private async Task<List<DiscountCode>> GetPublicVouchers()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(FirebaseDbUrl + "/discountCodes.json");
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var rawDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                        var result = new List<DiscountCode>();

                        if (rawDict != null)
                        {
                            foreach (var kv in rawDict)
                            {
                                if (kv.Value is JObject jobj)
                                {
                                    var code = jobj.ToObject<DiscountCode>();

                                    // CHỈ LẤY VOUCHER PUBLIC VÀ ACTIVE
                                    if (code != null && code.IsPublic && code.Active && code.IsValid(DateTime.Now))
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetPublicVouchers: {ex.Message}");
                return new List<DiscountCode>();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SaveDiscountCode(string voucherId)
        {
            var userId = Session["UserId"]?.ToString();
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(FirebaseDbUrl + $"/userVouchers/{userId}.json");
                    var codes = new List<string>();
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        codes = string.IsNullOrEmpty(json) || json == "null" 
                            ? new List<string>() 
                            : JsonConvert.DeserializeObject<List<string>>(json);
                    }

                    // Kiểm tra giới hạn voucher
                    if (codes.Count >= MaxVouchersPerUser)
                    {
                        TempData["ErrorMessage"] = $"Bạn chỉ được lưu tối đa {MaxVouchersPerUser} mã giảm giá!";
                        return RedirectToAction("PromotionNews");
                    }

                    if (codes.Contains(voucherId))
                    {
                        TempData["ErrorMessage"] = "Mã giảm giá đã được lưu trước đó!";
                        return RedirectToAction("PromotionNews");
                    }

                    // Kiểm tra voucher có tồn tại và hợp lệ không
                    var voucherResponse = await client.GetAsync($"{FirebaseDbUrl}/discountCodes/{voucherId}.json");
                    if (!voucherResponse.IsSuccessStatusCode)
                    {
                        TempData["ErrorMessage"] = "Mã giảm giá không tồn tại!";
                        return RedirectToAction("PromotionNews");
                    }

                    var voucherJson = await voucherResponse.Content.ReadAsStringAsync();
                    var voucher = JsonConvert.DeserializeObject<DiscountCode>(voucherJson);
                    
                    if (voucher == null || !voucher.Active || !voucher.IsPublic || !voucher.IsValid(DateTime.Now))
                    {
                        TempData["ErrorMessage"] = "Mã giảm giá không khả dụng!";
                        return RedirectToAction("PromotionNews");
                    }

                    // Thử lưu với retry logic
                    bool saveSuccess = false;
                    int attempt = 0;
                    
                    while (!saveSuccess && attempt < MaxVoucherSaveAttempts)
                    {
                        codes.Add(voucherId);
                        var content = new StringContent(JsonConvert.SerializeObject(codes), Encoding.UTF8, "application/json");
                        var putResponse = await client.PutAsync(FirebaseDbUrl + $"/userVouchers/{userId}.json", content);
                        
                        if (putResponse.IsSuccessStatusCode)
                        {
                            saveSuccess = true;
                            TempData["SuccessMessage"] = "Mã giảm giá đã được lưu thành công!";
                        }
                        else
                        {
                            attempt++;
                            await Task.Delay(200); // Đợi 200ms trước khi thử lại
                        }
                    }

                    if (!saveSuccess)
                    {
                        TempData["ErrorMessage"] = "Lỗi khi lưu mã giảm giá. Vui lòng thử lại!";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi hệ thống!";
                System.Diagnostics.Debug.WriteLine($"SaveDiscountCode error: {ex.Message}");
            }
            
            return RedirectToAction("PromotionNews");
        }

        public async Task<ActionResult> UserVouchers()
        {
            var userId = Session["UserId"]?.ToString();
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var vouchers = await GetSavedVouchersForUser(userId);
            return View(vouchers);
        }

        private async Task<List<DiscountCode>> GetSavedVouchersForUser(string userId)
        {
            var vouchers = new List<DiscountCode>();
            try
            {
                using (var client = new HttpClient())
                {
                    var res = await client.GetAsync($"{FirebaseDbUrl}/userVouchers/{userId}.json");
                    if (res.IsSuccessStatusCode)
                    {
                        var json = await res.Content.ReadAsStringAsync();
                        var codes = JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
                        
                        // Tải song song các voucher để tăng hiệu suất
                        var tasks = codes.Select(async idVoucher => 
                        {
                            var voucherRes = await client.GetAsync($"{FirebaseDbUrl}/discountCodes/{idVoucher}.json");
                            if (voucherRes.IsSuccessStatusCode)
                            {
                                var voucherJson = await voucherRes.Content.ReadAsStringAsync();
                                return JsonConvert.DeserializeObject<DiscountCode>(voucherJson);
                            }
                            return null;
                        });

                        var results = await Task.WhenAll(tasks);
                        vouchers = results
                            .Where(v => v != null && v.Active && v.IsValid(DateTime.Now))
                            .ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetSavedVouchersForUser error: {ex.Message}");
            }
            return vouchers;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveVoucher(string idVoucher)
        {
            var userId = Session["UserId"]?.ToString();
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            try
            {
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
                        TempData["SuccessMessage"] = "Đã xóa mã giảm giá!";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi xóa mã giảm giá!";
                System.Diagnostics.Debug.WriteLine($"RemoveVoucher error: {ex.Message}");
            }
            
            return RedirectToAction("UserVouchers");
        }

        public ActionResult CreateDiscountCode()
        {
            if (Session["UserRole"]?.ToString() != "Admin" && Session["UserId"] == null)
            {
                TempData["message"] = "Đi đâu vậy em !!! Không phải Admin mà đi vô đây chi ";

                return RedirectToAction("Login", "Account");
            }
            
            return View(new DiscountCode 
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMonths(1)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateDiscountCode(DiscountCode model)
        {
            if (Session["UserRole"]?.ToString() != "Admin")
                return RedirectToAction("Login", "Account");

            // Validate thủ công cho ngày tháng
            if (model.StartDate >= model.EndDate)
            {
                ModelState.AddModelError("EndDate", "Ngày kết thúc phải sau ngày bắt đầu");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    string idVoucher = Guid.NewGuid().ToString();
                    var discountData = new
                    {
                        IdVoucher = idVoucher,
                        CodeName = model.CodeName,
                        Condition = model.Condition,
                        DiscountType = model.DiscountType.ToString(),
                        DiscountValue = model.DiscountValue,
                        StartDate = model.StartDate.ToString("o"), // ISO 8601 format
                        EndDate = model.EndDate.ToString("o"),
                        Active = true,
                        IsPublic = model.IsPublic,
                        MinimumOrderValue = model.MinimumOrderValue
                    };

                    using (var client = new HttpClient())
                    {
                        var content = new StringContent(
                            JsonConvert.SerializeObject(discountData), 
                            Encoding.UTF8, 
                            "application/json");
                        
                        var response = await client.PutAsync(
                            $"{FirebaseDbUrl}/discountCodes/{idVoucher}.json", 
                            content);

                        if (response.IsSuccessStatusCode)
                        {
                            // Xóa cache khi có thay đổi
                            _publicVouchersCache = null;
                            
                            TempData["SuccessMessage"] = "Mã giảm giá đã được tạo thành công!";
                            return RedirectToAction("ManageDiscountCodes");
                        }
                        else
                        {
                            ModelState.AddModelError("", "Lỗi khi lưu mã giảm giá vào Firebase");
                        }
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Lỗi hệ thống: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"CreateDiscountCode error: {ex}");
                }
            }

            return View(model);
        }

        public async Task<ActionResult> ManageDiscountCodes()
        {
            if (Session["UserRole"]?.ToString() != "Admin")
                return RedirectToAction("Login", "Account");

            try
            {
                var discountCodes = await GetDiscountCodes();

                // Kiểm tra và gán ID cho các voucher
                foreach (var voucher in discountCodes)
                {
                    if (string.IsNullOrEmpty(voucher.IdVoucher))
                    {
                        // Tạo ID tạm thời nếu cần
                        voucher.IdVoucher = Guid.NewGuid().ToString();
                        System.Diagnostics.Debug.WriteLine($"Voucher without ID: {voucher.CodeName}");
                    }
                }

                if (!discountCodes.Any())
                {
                    TempData["InfoMessage"] = "Hiện chưa có mã giảm giá nào";
                }

                return View(discountCodes);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi tải danh sách mã giảm giá";
                System.Diagnostics.Debug.WriteLine($"ManageDiscountCodes error: {ex}");
                return View(new List<DiscountCode>());
            }
        }   
        private async Task<List<DiscountCode>> GetDiscountCodes()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(FirebaseDbUrl + "/discountCodes.json");
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();

                        // Xử lý trường hợp không có voucher nào
                        if (string.IsNullOrWhiteSpace(json) || json == "null")
                            return new List<DiscountCode>();

                        var discountDict = JsonConvert.DeserializeObject<Dictionary<string, DiscountCode>>(json);

                        return discountDict?
                            .Where(kvp => kvp.Value != null)
                            .Select(kvp => {
                                var voucher = kvp.Value;
                                voucher.IdVoucher = kvp.Key; // Đảm bảo ID được gán đúng
                                return voucher;
                            })
                            .ToList() ?? new List<DiscountCode>();
                    }
                    return new List<DiscountCode>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetDiscountCodes error: {ex}");
                return new List<DiscountCode>();
            }
        }
        // Helper methods for safe parsing
        private double ParseDouble(JToken token)
        {
            if (token == null) return 0;
            double.TryParse(token.ToString(), out double result);
            return result;
        }

        private DateTime ParseDateTime(JToken token)
        {
            if (token == null) return DateTime.MinValue;
            DateTime.TryParse(token.ToString(), out DateTime result);
            return result;
        }

        private bool ParseBool(JToken token)
        {
            if (token == null) return false;
            bool.TryParse(token.ToString(), out bool result);
            return result;
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> ToggleDiscountCode(string idVoucher, bool isActive)
        {
            try
            {
                // Kiểm tra quyền admin
                if (Session["UserRole"]?.ToString() != "Admin")
                {
                    return Json(new { success = false, message = "Bạn không có quyền thực hiện thao tác này" });
                }

                using (var client = new HttpClient())
                {
                    // Lấy voucher hiện tại từ Firebase
                    var voucherResponse = await client.GetAsync($"{FirebaseDbUrl}/discountCodes/{idVoucher}.json");
                    if (!voucherResponse.IsSuccessStatusCode)
                    {
                        return Json(new { success = false, message = "Voucher không tồn tại" });
                    }

                    var voucherJson = await voucherResponse.Content.ReadAsStringAsync();
                    var voucher = JsonConvert.DeserializeObject<DiscountCode>(voucherJson);

                    if (voucher == null)
                    {
                        return Json(new { success = false, message = "Voucher không tồn tại" });
                    }

                    // Cập nhật trạng thái
                    voucher.Active = isActive;
                    voucher.IsPublic = isActive; // Đảm bảo cả Active và IsPublic luôn đồng nhất

                    // Cập nhật toàn bộ voucher
                    var content = new StringContent(
                        JsonConvert.SerializeObject(voucher),
                        Encoding.UTF8,
                        "application/json");

                    var putResponse = await client.PutAsync(
                        $"{FirebaseDbUrl}/discountCodes/{idVoucher}.json",
                        content);

                    if (putResponse.IsSuccessStatusCode)
                    {
                        // Xóa cache để đảm bảo dữ liệu mới nhất
                        _publicVouchersCache = null;

                        return Json(new
                        {
                            success = true,
                            message = isActive
                                ? "Hiện mã giảm giá thành công!"
                                : "Ẩn mã giảm giá thành công!"
                        });
                    }
                    else
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"Lỗi khi cập nhật: {putResponse.StatusCode}"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ToggleDiscountCode error: {ex}");
                return Json(new
                {
                    success = false,
                    message = $"Lỗi hệ thống: {ex.Message}"
                });
            }
        }

        // Hàm helper lấy voucher theo ID
        private async Task<DiscountCode> GetVoucherById(string idVoucher)
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync($"{FirebaseDbUrl}/discountCodes/{idVoucher}.json");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<DiscountCode>(json);
                }
                return null;
            }
        }

        private async Task<bool> CheckVoucherExists(string idVoucher)
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync($"{FirebaseDbUrl}/discountCodes/{idVoucher}.json");
                return response.IsSuccessStatusCode;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> RemoveDiscountCode(string idVoucher)
        {
            try
            {
                if (Session["UserRole"]?.ToString() != "Admin")
                {
                    return Json(new { 
                        success = false, 
                        message = "Bạn không có quyền thực hiện hành động này." 
                    });
                }

                using (var client = new HttpClient())
                {
                    var response = await client.DeleteAsync(
                        $"{FirebaseDbUrl}/discountCodes/{idVoucher}.json");

                    if (response.IsSuccessStatusCode)
                    {
                        // Xóa cache khi có thay đổi
                        _publicVouchersCache = null;
                        
                        return Json(new { 
                            success = true, 
                            message = "Mã giảm giá đã được xóa thành công!" 
                        });
                    }
                    else
                    {
                        return Json(new { 
                            success = false, 
                            message = "Không thể xóa mã giảm giá." 
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RemoveDiscountCode error: {ex}");
                return Json(new { 
                    success = false, 
                    message = $"Lỗi hệ thống: {ex.Message}" 
                });
            }
        }
    }
}
