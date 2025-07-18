using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using SneakerSportStore.Models;
using System.Web.Security;  
using System.Web; 
using System.ComponentModel.DataAnnotations;
using static SneakerSportStore.Controllers.SettingsController;

namespace SneakerSportStore.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private const string FirebaseApiKey = "AIzaSyCETbgg3mnPMIMha_KYFmFauleZwV6CSo4";
        private const string FirebaseDbUrl = "https://sneakersportstore-default-rtdb.asia-southeast1.firebasedatabase.app";
        private const string FirebaseAuthUrl = "https://identitytoolkit.googleapis.com/v1/accounts:";

        private readonly HttpClient _httpClient;

        public AccountController()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(FirebaseDbUrl);
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //[AllowAnonymous]
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<ActionResult> Login(LoginModel model, string returnUrl)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return View(model);
        //    }

        //    try
        //    {
        //        var payload = new
        //        {
        //            email = model.Email,
        //            password = model.Password,
        //            returnSecureToken = true
        //        };

        //        var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
        //        var response = await _httpClient.PostAsync($"{FirebaseAuthUrl}signInWithPassword?key={FirebaseApiKey}", content);

        //        if (!response.IsSuccessStatusCode)
        //        {
        //            ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không chính xác.");
        //            return View(model);
        //        }

        //        var responseBody = await response.Content.ReadAsStringAsync();
        //        dynamic result = JsonConvert.DeserializeObject(responseBody);
        //        string userId = result.localId;

        //         var userInfoResponse = await _httpClient.GetAsync($"{FirebaseDbUrl}/users/{userId}.json");
        //        var userInfo = await userInfoResponse.Content.ReadAsStringAsync();
        //        dynamic userData = JsonConvert.DeserializeObject(userInfo);

        //         Session["FirebaseToken"] = result.idToken;
        //        Session["UserId"] = userId;
        //        Session["UserRole"] = userData?.userRole ?? "User";
        //        Session["Email"] = result.email;
        //        Session["FullName"] = userData?.hoTen ?? "";
        //        Session["TenDangNhap"] = userData?.tenDangNhap ?? "";

        //         var ticket = new FormsAuthenticationTicket(
        //            version: 1,
        //            name: model.Email,
        //            issueDate: DateTime.Now,
        //            expiration: DateTime.Now.AddMinutes(30),
        //            isPersistent: false,
        //            userData: userId
        //        );

        //        var encryptedTicket = FormsAuthentication.Encrypt(ticket);
        //        var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
        //        Response.Cookies.Add(authCookie);

        //        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        //        {
        //            return Redirect(returnUrl);
        //        }
        //        return RedirectToAction("Index", "Home");
        //    }
        //    catch (Exception ex)
        //    {
        //        ModelState.AddModelError("", $"Đã xảy ra lỗi: {ex.Message}");
        //        return View(model);
        //    }
        //}

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginModel model, string returnUrl)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var payload = new
                {
                    email = model.Email,
                    password = model.Password,
                    returnSecureToken = true
                };

                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{FirebaseAuthUrl}signInWithPassword?key={FirebaseApiKey}", content);

                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không chính xác.");
                    return View(model);
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject(responseBody);
                string userId = result.localId;

                var userInfoResponse = await _httpClient.GetAsync($"{FirebaseDbUrl}/users/{userId}.json");
                var userInfo = await userInfoResponse.Content.ReadAsStringAsync();
                dynamic userData = JsonConvert.DeserializeObject(userInfo) ?? new
                {
                    userRole = "User",
                    hoTen = "",
                    tenDangNhap = ""
                };

                // Lưu session
                Session["FirebaseToken"] = result.idToken;
                Session["UserId"] = userId;
                Session["UserRole"] = userData.userRole?.ToString() ?? "User";
                Session["Email"] = result.email?.ToString() ?? "";
                Session["FullName"] = userData.hoTen?.ToString() ?? "";
                Session["TenDangNhap"] = userData.tenDangNhap?.ToString() ?? "";

                // Tạo authentication ticket
                var ticket = new FormsAuthenticationTicket(
                    version: 1,
                    name: userId, // Sử dụng userId thay vì email
                    issueDate: DateTime.Now,
                    expiration: DateTime.Now.AddMinutes(30),
                    isPersistent: false,
                    userData: Session["UserRole"]?.ToString() ?? "User" // Lưu role vào UserData
                );

                var encryptedTicket = FormsAuthentication.Encrypt(ticket);
                var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket)
                {
                    HttpOnly = true,
                    Secure = Request.IsSecureConnection,
                    Domain = Request.Url?.Host,
                    Path = "/"
                };

                if (ticket.IsPersistent)
                {
                    authCookie.Expires = ticket.Expiration;
                }

                Response.Cookies.Add(authCookie);

                // Xử lý URL trở về
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Đã xảy ra lỗi: {ex.Message}");
                return View(model);
            }
        }


        [AllowAnonymous]
        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                 var authPayload = new
                {
                    email = model.Email,
                    password = model.Password,
                    returnSecureToken = true
                };

                var authContent = new StringContent(JsonConvert.SerializeObject(authPayload), Encoding.UTF8, "application/json");
                var authResponse = await _httpClient.PostAsync($"{FirebaseAuthUrl}signUp?key={FirebaseApiKey}", authContent);

                if (!authResponse.IsSuccessStatusCode)
                {
                    var errorBody = await authResponse.Content.ReadAsStringAsync();
                    dynamic errorResult = JsonConvert.DeserializeObject(errorBody);
                    ModelState.AddModelError("", $"Lỗi đăng ký: {errorResult?.error?.message ?? "Không xác định"}");
                    return View(model);
                }

                var authResponseBody = await authResponse.Content.ReadAsStringAsync();
                dynamic authResult = JsonConvert.DeserializeObject(authResponseBody);
                string userId = authResult.localId;

                 var userData = new
                {
                    userId,
                    hoTen = model.HoTen,
                    email = model.Email,
                    userRole = "User",
                    soDienThoai = model.SoDienThoai,
                    diaChi = model.DiaChi,
                    tenDangNhap = model.TenDangNhap
                };

                var dbContent = new StringContent(JsonConvert.SerializeObject(userData), Encoding.UTF8, "application/json");
                await _httpClient.PutAsync($"{FirebaseDbUrl}/users/{userId}.json", dbContent);

                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Đã xảy ra lỗi: {ex.Message}");
                return View(model);
            }
        }
        [HttpPost]
        public ActionResult Logout()
        {
            Session.Clear();
            TempData["SuccessMessage"] = "Bạn đã đăng xuất thành công.";
            return RedirectToAction("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteAccount()
        {
            var userId = Session["UserId"]?.ToString();
            var idToken = Session["FirebaseToken"]?.ToString();

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(idToken))
            {
                return RedirectToAction("Login");
            }

            try
            {
                 await _httpClient.DeleteAsync($"{FirebaseDbUrl}/users/{userId}.json");

                 var deletePayload = new { idToken };
                var deleteContent = new StringContent(JsonConvert.SerializeObject(deletePayload), Encoding.UTF8, "application/json");
                await _httpClient.PostAsync($"{FirebaseAuthUrl}delete?key={FirebaseApiKey}", deleteContent);

                 Session.Clear();
                FormsAuthentication.SignOut();

                TempData["SuccessMessage"] = "Tài khoản đã được xóa thành công.";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi xóa tài khoản: {ex.Message}";
                return RedirectToAction("Profile", "User");
            }
        }
        [AllowAnonymous]
        [HttpGet]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var payload = new
                {
                    requestType = "PASSWORD_RESET",
                    email = model.Email
                };

                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{FirebaseAuthUrl}sendOobCode?key={FirebaseApiKey}", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    dynamic errorResult = JsonConvert.DeserializeObject(errorBody);
                    ModelState.AddModelError("", $"Lỗi: {errorResult?.error?.message ?? "Không thể gửi email đặt lại mật khẩu"}");
                    return View(model);
                }

                TempData["SuccessMessage"] = "Email đặt lại mật khẩu đã được gửi. Vui lòng kiểm tra hộp thư của bạn.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Đã xảy ra lỗi: {ex.Message}");
                return View(model);
            }
        }

        [HttpGet]
        public ActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var idToken = Session["FirebaseToken"]?.ToString();
            if (string.IsNullOrEmpty(idToken))
            {
                TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login");
            }

            try
            {
                var payload = new
                {
                    idToken = idToken,
                    password = model.NewPassword,
                    returnSecureToken = true
                };

                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{FirebaseAuthUrl}update?key={FirebaseApiKey}", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    dynamic errorResult = JsonConvert.DeserializeObject(errorBody);
                    ModelState.AddModelError("", $"Lỗi: {errorResult?.error?.message ?? "Không thể thay đổi mật khẩu"}");
                    return View(model);
                }

                // Cập nhật token mới nếu có
                var responseBody = await response.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject(responseBody);
                if (result.idToken != null)
                {
                    Session["FirebaseToken"] = result.idToken;
                }

                TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
                return RedirectToAction("Profile", "User");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Đã xảy ra lỗi: {ex.Message}");
                return View(model);
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult ResetPassword(string oobCode)
        {
            if (string.IsNullOrEmpty(oobCode))
            {
                TempData["ErrorMessage"] = "Mã đặt lại mật khẩu không hợp lệ.";
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordModel { OobCode = oobCode };
            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var payload = new
                {
                    oobCode = model.OobCode,
                    newPassword = model.NewPassword
                };

                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{FirebaseAuthUrl}resetPassword?key={FirebaseApiKey}", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    dynamic errorResult = JsonConvert.DeserializeObject(errorBody);
                    ModelState.AddModelError("", $"Lỗi: {errorResult?.error?.message ?? "Không thể đặt lại mật khẩu"}");
                    return View(model);
                }

                TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập bằng mật khẩu mới.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Đã xảy ra lỗi: {ex.Message}");
                return View(model);
            }
        }
    }


}

 

   
