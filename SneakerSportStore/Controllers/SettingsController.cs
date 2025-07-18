using Newtonsoft.Json;
using SneakerSportStore.Extensions;
using SneakerSportStore.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SneakerSportStore.Controllers
{
    public class SettingsController : Controller
    {
        private readonly string FirebaseDbUrl = "https://sneakersportstore-default-rtdb.asia-southeast1.firebasedatabase.app";
        private readonly string FirebaseApiKey = "AIzaSyCETbgg3mnPMIMha_KYFmFauleZwV6CSo4";
        private readonly string TwilioAccountSid = "your_account_sid";
        private readonly string TwilioAuthToken = "your_auth_token"; 
        private readonly string TwilioPhoneNumber = "0799192226";


        

//        public class DashboardViewModel
//{
//    public UserInfo UserInfo { get; set; }
//    public List<UserActivity> RecentActivities { get; set; }
//    public int OrderCount { get; set; }
//    public int UnreadNotifications { get; set; }
//}
        public ActionResult Index()
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Account");

            return View();
        }
        public class ApiResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public object Data { get; set; }
        }


        [HttpGet]
        public async Task<JsonResult> ActivityLog()
        {
            var responseObj = new ApiResponse();
            var userId = Session["UserId"]?.ToString();

            if (string.IsNullOrEmpty(userId))
            {
                responseObj.Success = false;
                responseObj.Message = "Yêu cầu đăng nhập";
                return Json(responseObj, JsonRequestBehavior.AllowGet);
            }

            using (var client = new HttpClient())
            {
                try
                {
                    var response = await client.GetAsync($"{FirebaseDbUrl}/activity/{userId}.json");
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync();
                    var activities = JsonConvert.DeserializeObject<List<UserActivity>>(json) ?? new List<UserActivity>();

                    responseObj.Success = true;
                    responseObj.Data = activities;
                }
                catch (Exception ex)
                {
                    responseObj.Success = false;
                    responseObj.Message = $"Lỗi khi tải nhật ký: {ex.Message}";
                }
            }

            return Json(responseObj, JsonRequestBehavior.AllowGet);
        }
        public class UserActivity
        {
            public DateTime Timestamp { get; set; }
            public string Action { get; set; }
            public string Details { get; set; }
        }
        public async Task<ActionResult> EditProfile()
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Account");

            var userId = Session["UserId"].ToString();
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync($"{FirebaseDbUrl}/users/{userId}.json");
                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Không thể tải dữ liệu người dùng.";
                    return View();
                }

                var json = await response.Content.ReadAsStringAsync();
                var user = JsonConvert.DeserializeObject<dynamic>(json);
                ViewBag.UserId = userId;
                return View(user);
            }
        }

        [HttpPost]
        public async Task<ActionResult> EditProfile(string userId, string hoTen, string email, string soDienThoai, string diaChi)
        {
            using (var client = new HttpClient())
            {
                var updateData = new
                {
                    hoTen,
                    email,
                    soDienThoai,
                    diaChi
                };

                var content = new StringContent(JsonConvert.SerializeObject(updateData), Encoding.UTF8, "application/json");
                var response = await client.PatchAsync($"{FirebaseDbUrl}/users/{userId}.json", content);

                TempData["Message"] = response.IsSuccessStatusCode ? "Cập nhật thành công!" : "Lỗi khi cập nhật!";
                return RedirectToAction("EditProfile");
            }
        }

        [HttpPost]
        public async Task<ActionResult> ForgotPassword(string soDienThoai)
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync($"{FirebaseDbUrl}/users.json");
                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Message = "Lỗi máy chủ!";
                    return View();
                }

                var json = await response.Content.ReadAsStringAsync();
                var users = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(json);

                string userId = null;
                foreach (var u in users)
                {
                    if ((string)u.Value.soDienThoai == soDienThoai)
                    {
                        userId = u.Key;
                        break;
                    }
                }

                if (userId == null)
                {
                    ViewBag.Message = "Không tìm thấy tài khoản với số điện thoại này.";
                    return View();
                }

                string otp = new Random().Next(100000, 999999).ToString();
                Session["ResetUserId"] = userId;
                Session["ResetOtp"] = otp;
                Session["OtpTimeout"] = DateTime.Now.AddMinutes(5);

                SendOtpViaSms(soDienThoai, otp);

                ViewBag.OtpSent = true;
                ViewBag.SoDienThoai = soDienThoai;
                ViewBag.Message = "Đã gửi mã OTP, vui lòng kiểm tra điện thoại!";
                return View();
            }
        }

        private void SendOtpViaSms(string phoneNumber, string otp)
        {
            try
            {
                Twilio.TwilioClient.Init(TwilioAccountSid, TwilioAuthToken);

                var message = Twilio.Rest.Api.V2010.Account.MessageResource.Create(
                    to: new Twilio.Types.PhoneNumber(phoneNumber),
                    from: new Twilio.Types.PhoneNumber(TwilioPhoneNumber),
                    body: $"Mã OTP của bạn là: {otp}"
                );
                Console.WriteLine($"OTP đã gửi: {message.Sid}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi gửi OTP: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<ActionResult> VerifyOtpAndReset(string soDienThoai, string otp, string newPassword, string rePassword)
        {
            if (newPassword != rePassword)
            {
                ViewBag.Message = "Mật khẩu không khớp.";
                return View("ForgotPassword");
            }

            var savedOtp = Session["ResetOtp"] as string;
            var timeout = Session["OtpTimeout"] as DateTime?;
            var userId = Session["ResetUserId"] as string;

            if (savedOtp == null || otp != savedOtp || timeout == null || DateTime.Now > timeout)
            {
                ViewBag.Message = "Mã OTP không hợp lệ hoặc đã hết hạn!";
                return View("ForgotPassword");
            }

            using (var client = new HttpClient())
            {
                var updateData = new { matKhau = newPassword };
                var content = new StringContent(JsonConvert.SerializeObject(updateData), Encoding.UTF8, "application/json");
                var response = await client.PatchAsync($"{FirebaseDbUrl}/users/{userId}.json", content);

                if (response.IsSuccessStatusCode)
                {
                    ViewBag.Message = "Đặt lại mật khẩu thành công!";
                }
                else
                {
                    ViewBag.Message = "Lỗi khi đặt lại mật khẩu!";
                }

                Session.Remove("ResetOtp");
                Session.Remove("OtpTimeout");
                Session.Remove("ResetUserId");
                return View("ForgotPassword");
            }
        }

        public ActionResult ChangePassword()
        {
            return View();
        }

        [HttpGet]
        public ActionResult ForgotPassword()
        {
            return View();
        }
        public async Task<ActionResult> Dashboard()
        {
            var userId = Session["UserId"]?.ToString();
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Không tìm thấy thông tin người dùng.";
                return RedirectToAction("Index");
            }

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync($"{FirebaseDbUrl}/users/{userId}.json");
                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Không thể tải thông tin người dùng.";
                    return RedirectToAction("Index");
                }

                var json = await response.Content.ReadAsStringAsync();
                var user = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(json);

                if (user == null)
                {
                    TempData["Error"] = "Không có thông tin người dùng.";
                    return RedirectToAction("Index");
                }

                var hoTen = user.ContainsKey("hoTen") ? user["hoTen"] : "Chưa cập nhật";
                var email = user.ContainsKey("email") ? user["email"] : "Chưa cập nhật";
                var soDienThoai = user.ContainsKey("soDienThoai") ? user["soDienThoai"] : "Chưa cập nhật";
                var diaChi = user.ContainsKey("diaChi") ? user["diaChi"] : "Chưa cập nhật";
                var tenDangNhap = user.ContainsKey("tenDangNhap") ? user["tenDangNhap"] : "Chưa cập nhật";

                var userInfo = new UserInfo
                {
                    HoTen = hoTen,
                    Email = email,
                    SoDienThoai = soDienThoai,
                    DiaChi = diaChi,
                    TenDangNhap=tenDangNhap
                };

                ViewBag.User = userInfo; 

                return View();
            }
        }
        // POST: Settings/ChangePassword
        [HttpPost]
        public async Task<JsonResult> ChangePassword(ChangePasswordModel model)
        {
            var responseObj = new ApiResponse();
            var userId = Session["UserId"]?.ToString();

            if (string.IsNullOrEmpty(userId))
            {
                responseObj.Success = false;
                responseObj.Message = "Phiên đăng nhập hết hạn";
                return Json(responseObj);
            }

            if (model.NewPassword != model.ConfirmPassword)
            {
                responseObj.Success = false;
                responseObj.Message = "Mật khẩu mới không khớp";
                return Json(responseObj);
            }

            using (var client = new HttpClient())
            {
                try
                {
                    // Verify current password
                    var response = await client.GetAsync($"{FirebaseDbUrl}/users/{userId}.json");
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync();
                    var user = JsonConvert.DeserializeObject<dynamic>(json);

                    if (model.CurrentPassword != (string)user.matKhau)
                    {
                        responseObj.Success = false;
                        responseObj.Message = "Mật khẩu hiện tại không đúng";
                        return Json(responseObj);
                    }

                    // Update password
                    var updateData = new { matKhau = model.NewPassword };
                    var content = new StringContent(
                        JsonConvert.SerializeObject(updateData),
                        Encoding.UTF8,
                        "application/json"
                    );

                    var updateResponse = await client.PatchAsync($"{FirebaseDbUrl}/users/{userId}.json", content);
                    updateResponse.EnsureSuccessStatusCode();

                    responseObj.Success = true;
                    responseObj.Message = "Đổi mật khẩu thành công";
                }
                catch (Exception ex)
                {
                    responseObj.Success = false;
                    responseObj.Message = $"Lỗi khi đổi mật khẩu: {ex.Message}";
                }
            }

            return Json(responseObj);
        }
        //public class ChangePasswordModel
        //{
        //    public string CurrentPassword { get; set; }
        //    public string NewPassword { get; set; }
        //    public string ConfirmPassword { get; set; }
        //}

        [HttpPost]
        public async Task<ActionResult> DeleteAccount(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Không tìm thấy tài khoản!";
                return RedirectToAction("Index");
            }

            using (var client = new HttpClient())
            {
                var response = await client.DeleteAsync($"{FirebaseDbUrl}/users/{userId}.json");
                if (response.IsSuccessStatusCode)
                {
                    TempData["Message"] = "Tài khoản đã bị xóa thành công!";
                    // Xóa session nếu có
                    Session.Clear();
                }
                else
                {
                    TempData["Error"] = "Lỗi khi xóa tài khoản!";
                }
            }

            return RedirectToAction("Index");
        }
    }
}
