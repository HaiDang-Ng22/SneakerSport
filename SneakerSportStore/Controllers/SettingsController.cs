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
        private readonly string TwilioAuthToken = "your_auth_token"; // Auth Token từ Twilio
        private readonly string TwilioPhoneNumber = "0799192226"; // Thay bằng số điện thoại Twilio của bạn

        // Trang chính của Settings
        public ActionResult Index()
        {
            if (Session["CustomerID"] == null)
                return RedirectToAction("Login", "Account");

            return View();
        }

        // Trang cập nhật thông tin cá nhân
        public async Task<ActionResult> EditProfile()
        {
            if (Session["CustomerID"] == null)
                return RedirectToAction("Login", "Account");

            var userId = Session["CustomerID"].ToString();
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

        // Gửi mã OTP cho người dùng
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

                // Gửi mã OTP tới số điện thoại
                string otp = new Random().Next(100000, 999999).ToString();
                Session["ResetUserId"] = userId;
                Session["ResetOtp"] = otp;
                Session["OtpTimeout"] = DateTime.Now.AddMinutes(5);

                // Thực tế, bạn cần gửi OTP qua dịch vụ SMS như Twilio
                SendOtpViaSms(soDienThoai, otp);

                ViewBag.OtpSent = true;
                ViewBag.SoDienThoai = soDienThoai;
                ViewBag.Message = "Đã gửi mã OTP, vui lòng kiểm tra điện thoại!";
                return View();
            }
        }

        // Gửi mã OTP qua Twilio
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

        // Kiểm tra OTP và cập nhật mật khẩu
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
                // Cập nhật mật khẩu cho người dùng
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

                // Xóa session OTP
                Session.Remove("ResetOtp");
                Session.Remove("OtpTimeout");
                Session.Remove("ResetUserId");
                return View("ForgotPassword");
            }
        }

        // Trang đổi mật khẩu
        public ActionResult ChangePassword()
        {
            return View();
        }

        // GET: ForgotPassword
        [HttpGet]
        public ActionResult ForgotPassword()
        {
            return View();
        }
        public async Task<ActionResult> DetailsUser()
        {
            var userId = Session["CustomerID"]?.ToString();
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

                // Gán vào model UserInfo
                var userInfo = new UserInfo
                {
                    HoTen = hoTen,
                    Email = email,
                    SoDienThoai = soDienThoai,
                    DiaChi = diaChi
                };

                ViewBag.User = userInfo; // Gán vào ViewBag

                return View();
            }
        }

        // Xóa tài khoản người dùng
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
