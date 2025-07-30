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
using SneakerSportStore.Extensions;
using SneakerSportStore.Services;
using static System.Net.WebRequestMethods;
using System.IO;
using System.Net;

namespace SneakerSportStore.Controllers
{

    [Authorize]
    public class AccountController : Controller
    {
        private const string FirebaseApiKey = "AIzaSyCETbgg3mnPMIMha_KYFmFauleZwV6CSo4";
        private const string FirebaseDbUrl = "https://sneakersportstore-default-rtdb.asia-southeast1.firebasedatabase.app/";
        private const string FirebaseAuthUrl = "https://identitytoolkit.googleapis.com/v1/accounts:";
        private readonly IOtpSender _otpSender;

        private const int OtpLength = 6;
        private const int OtpExpireMinutes = 5;
        private const int OtpMaxAttempts = 5;
        private const int OtpResendCooldownSeconds = 30;

      
        private readonly HttpClient _http; 
        public AccountController() : this(new EmailOtpSender()) { }

        public AccountController(IOtpSender otpSender)
        {
            _otpSender = otpSender;
            _http = new HttpClient { BaseAddress = new Uri(FirebaseDbUrl) };
            //_http = new HttpClient();

        }

        //private readonly HttpClient _httpClient;



        private string GenerateOtp()
        {
            var r = new Random();
            var sb = new StringBuilder(OtpLength);
            for (int i = 0; i < OtpLength; i++) sb.Append(r.Next(0, 10));
            return sb.ToString();
        }

        private string EmailKey(string email) => email.Replace("@", "_at_").Replace(".", "_dot_");



        [AllowAnonymous]
        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginModel());
        }



        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            try
            {
                var payload = new
                {
                    email = model.Email.Trim().ToLowerInvariant(),
                    password = model.Password,
                    returnSecureToken = true
                };
                var content = new StringContent(
                    JsonConvert.SerializeObject(payload),
                    Encoding.UTF8,
                    "application/json"
                );
                var signInResp = await _http.PostAsync(
                    $"{FirebaseAuthUrl}signInWithPassword?key={FirebaseApiKey}",
                    content
                );
                if (!signInResp.IsSuccessStatusCode)
                {
                    ModelState.AddModelError("", "Email hoặc mật khẩu không chính xác.");
                    ViewBag.ReturnUrl = returnUrl;
                    return View(model);
                }

                var signInBody = await signInResp.Content.ReadAsStringAsync();
                dynamic signInResult = JsonConvert.DeserializeObject(signInBody);
                string userId = signInResult.localId;
                string idToken = signInResult.idToken;
                string email = signInResult.email;

                var userInfoResp = await _http.GetAsync($"{FirebaseDbUrl}/users/{userId}.json");
                if (userInfoResp.StatusCode == HttpStatusCode.NotFound)
                {
                    ModelState.AddModelError("", "Không tìm thấy thông tin người dùng.");
                    ViewBag.ReturnUrl = returnUrl;
                    return View(model);
                }
                var userJson = await userInfoResp.Content.ReadAsStringAsync();
                dynamic userData = JsonConvert.DeserializeObject(userJson);

                string role = userData.userRole != null ? (string)userData.userRole : "User";
                string fullName = userData.hoTen != null ? (string)userData.hoTen : "";

                Session["FullName"] = fullName;
                Session["UserRole"] = role;
                Session["FirebaseToken"] = idToken;
                Session["UserId"] = userId;
                Session["Email"] = email;

                

                var ticket = new FormsAuthenticationTicket(
                    version: 1,
                    name: userId,
                    issueDate: DateTime.Now,
                    expiration: DateTime.Now.AddMinutes(30),
                    isPersistent: model.RememberMe,
                    userData: role,
                    cookiePath: FormsAuthentication.FormsCookiePath
                );
                string encryptedTicket = FormsAuthentication.Encrypt(ticket);

                var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket)
                {
                    HttpOnly = true,
                    Path = FormsAuthentication.FormsCookiePath
                };
                if (ticket.IsPersistent)
                    authCookie.Expires = ticket.Expiration;

                Response.Cookies.Add(authCookie);

                if (!String.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Đã xảy ra lỗi: " + ex.Message);
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }
        }


        [AllowAnonymous]
        [HttpGet]
        public ActionResult Register()
        {
            return View(new RegisterModel { Step = "EMAIL" });
        }
        

        [AllowAnonymous]
        [HttpPost]
        public async Task<JsonResult> CheckEmail()
        {
            try
            {
                Request.InputStream.Position = 0;
                using (var sr = new StreamReader(Request.InputStream))
                {
                    var raw = sr.ReadToEnd();
                    dynamic obj = JsonConvert.DeserializeObject(raw);
                    string email = (string)obj?.email;   
                    email = (email ?? "").Trim().ToLowerInvariant();

                    if (string.IsNullOrEmpty(email))
                        return Json(new { exists = false, message = "Email trống." });

                    var exists = await EmailExistsAsync(email);
                    return Json(new { exists, message = exists ? "Email đã đăng ký" : "OK" });
                }
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class CheckEmailDto { public string Email { get; set; } }

        [AllowAnonymous]
        [HttpPost]
        public async Task<JsonResult> SendOtp(string email)
        {
            try
            {
                email = email?.Trim().ToLowerInvariant();
                if (string.IsNullOrEmpty(email) || !new EmailAddressAttribute().IsValid(email))
                    return Json(new { success = false, message = "Email không hợp lệ." });

                if (await EmailExistsAsync(email))
                    return Json(new { success = false, message = "Email này đã được đăng ký." });

                var key = EmailKey(email);
                var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                var existingResp = await _http.GetAsync($"/pendingRegistrations/{key}.json");
                if (existingResp.IsSuccessStatusCode)
                {
                    var existingBody = await existingResp.Content.ReadAsStringAsync();
                    if (existingBody != "null")
                    {
                        dynamic pend = JsonConvert.DeserializeObject(existingBody);
                        long expiresAt = pend.expiresAt;
                        long lastSent = pend.lastSent;
                        if (nowMs < expiresAt)
                        {
                            var diff = nowMs - lastSent;
                            if (diff < OtpResendCooldownSeconds * 1000)
                            {
                                int remain = (int)((OtpResendCooldownSeconds * 1000 - diff) / 1000);
                                return Json(new { success = false, message = $"Vui lòng chờ {remain}s để gửi lại OTP.", cooldown = remain });
                            }
                        }
                    }
                }

                string otp = GenerateOtp();
                var pendingObj = new
                {
                    email,
                    otp,
                    expiresAt = DateTimeOffset.UtcNow.AddMinutes(OtpExpireMinutes).ToUnixTimeMilliseconds(),
                    attempts = 0,
                    resendCount = 0,
                    lastSent = nowMs,
                    verified = false,

                };

                var put = await _http.PutAsync($"/pendingRegistrations/{key}.json",
                    new StringContent(JsonConvert.SerializeObject(pendingObj), Encoding.UTF8, "application/json"));

                if (!put.IsSuccessStatusCode)
                    return Json(new { success = false, message = "Không thể lưu phiên OTP." });

                try
                {
                    await _otpSender.SendOtpAsync(email, otp);
                    return Json(new { success = true, message = "Đã gửi OTP.", cooldown = OtpResendCooldownSeconds });
                }
                catch
                {
                    await _http.DeleteAsync($"/pendingRegistrations/{key}.json");
                    return Json(new { success = false, message = "Không gửi được email OTP. Thử lại." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }




        //[AllowAnonymous]
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<ActionResult> Register(RegisterModel model, string actionType)
        //{
        //    model.Email = model.Email?.Trim().ToLowerInvariant();
        //    if (string.IsNullOrEmpty(model.Step)) model.Step = "EMAIL";

        //    // --- STEP 1: Gửi OTP ---
        //    if (model.Step == "EMAIL" && actionType == "SEND_EMAIL")
        //    {
        //        ModelState.Clear();

        //        if (string.IsNullOrWhiteSpace(model.Email) ||
        //            !new EmailAddressAttribute().IsValid(model.Email))
        //        {
        //            ModelState.AddModelError("Email", "Email không hợp lệ.");
        //            model.Step = "EMAIL";
        //            return View(model);
        //        }

        //        if (await EmailExistsAsync(model.Email))
        //        {
        //            ModelState.AddModelError("Email", "Gmail này đã được đăng ký.");
        //            model.Step = "EMAIL";
        //            return View(model);
        //        }

        //        var otp = GenerateOtp();
        //        var now = DateTime.UtcNow;
        //        var pending = new
        //        {
        //            email = model.Email,
        //            otp,
        //            expiresAt = new DateTimeOffset(now.AddMinutes(OtpExpireMinutes)).ToUnixTimeMilliseconds(),
        //            attempts = 0,
        //            resendCount = 0,
        //            lastSent = new DateTimeOffset(now).ToUnixTimeMilliseconds()
        //        };
        //        var key = EmailKey(model.Email);
        //        var save = await _http.PutAsync($"/pendingRegistrations/{key}.json",
        //            new StringContent(JsonConvert.SerializeObject(pending), Encoding.UTF8, "application/json"));
        //        if (!save.IsSuccessStatusCode)
        //        {
        //            ModelState.AddModelError("", "Không tạo được OTP. Thử lại.");
        //            model.Step = "EMAIL";
        //            return View(model);
        //        }

        //        try { await _otpSender.SendOtpAsync(model.Email, otp); }
        //        catch
        //        {
        //            await _http.DeleteAsync($"/pendingRegistrations/{key}.json");
        //            ModelState.AddModelError("", "Không gửi được OTP. Thử lại sau.");
        //            model.Step = "EMAIL";
        //            return View(model);
        //        }

        //        TempData["Info"] = "Đã gửi mã OTP tới email của bạn.";
        //        model.Step = "OTP";
        //        model.SecondsToResend = OtpResendCooldownSeconds;
        //        return View(model);
        //    }

        //    // --- RESEND OTP ---
        //    if (model.Step == "OTP" && actionType == "RESEND")
        //    {
        //        var key = EmailKey(model.Email);
        //        var resp = await _http.GetAsync($"/pendingRegistrations/{key}.json");
        //        var body = await resp.Content.ReadAsStringAsync();
        //        if (body == "null")
        //        {
        //            ModelState.Clear();
        //            ModelState.AddModelError("", "OTP hết hạn. Hãy nhập lại email.");
        //            model.Step = "EMAIL";
        //            return View(model);
        //        }

        //        dynamic p = JsonConvert.DeserializeObject(body);
        //        long lastSent = p.lastSent;
        //        long expiresAt = p.expiresAt;
        //        int resendCount = p.resendCount ?? 0;
        //        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        //        if (nowMs > expiresAt)
        //        {
        //            await _http.DeleteAsync($"/pendingRegistrations/{key}.json");
        //            ModelState.Clear();
        //            ModelState.AddModelError("", "OTP hết hạn. Bắt đầu lại.");
        //            model.Step = "EMAIL";
        //            return View(model);
        //        }

        //        if ((nowMs - lastSent) < OtpResendCooldownSeconds * 1000)
        //        {
        //            var remain = (int)((OtpResendCooldownSeconds * 1000 - (nowMs - lastSent)) / 1000);
        //            ModelState.Clear();
        //            ModelState.AddModelError("", $"Vui lòng chờ {remain}s để gửi lại.");
        //            model.Step = "OTP";
        //            model.SecondsToResend = remain;
        //            return View(model);
        //        }

        //        var newOtp = GenerateOtp();
        //        var patch = new
        //        {
        //            otp = newOtp,
        //            lastSent = nowMs,
        //            attempts = 0,
        //            resendCount = resendCount + 1
        //        };
        //        await _http.PatchAsync($"/pendingRegistrations/{key}.json",
        //            new StringContent(JsonConvert.SerializeObject(patch), Encoding.UTF8, "application/json"));
        //        try { await _otpSender.SendOtpAsync(model.Email, newOtp); TempData["Info"] = "Đã gửi OTP mới."; }
        //        catch { ModelState.AddModelError("", "Không gửi được OTP mới."); }
        //        model.Step = "OTP";
        //        model.SecondsToResend = OtpResendCooldownSeconds;
        //        return View(model);
        //    }

        //    // --- STEP 2: Xác thực OTP ---
        //    if (model.Step == "OTP" && actionType == "VERIFY_OTP")
        //    {
        //        ModelState.Clear();
        //        if (string.IsNullOrWhiteSpace(model.Otp) || model.Otp.Length != 6)
        //        {
        //            ModelState.AddModelError("Otp", "Vui lòng nhập OTP 6 số.");
        //            model.Step = "OTP";
        //            return View(model);
        //        }

        //        var key = EmailKey(model.Email);
        //        var pendingResp = await _http.GetAsync($"/pendingRegistrations/{key}.json");
        //        var pendingBody = await pendingResp.Content.ReadAsStringAsync();
        //        if (pendingBody == "null")
        //        {
        //            ModelState.AddModelError("", "OTP hết hạn hoặc không tồn tại.");
        //            model.Step = "EMAIL";
        //            return View(model);
        //        }

        //        dynamic pending = JsonConvert.DeserializeObject(pendingBody);
        //        string otpStored = pending.otp;
        //        long expiresAt = pending.expiresAt;
        //        int attempts = pending.attempts;
        //        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        //        if (nowMs > expiresAt)
        //        {
        //            await _http.DeleteAsync($"/pendingRegistrations/{key}.json");
        //            ModelState.AddModelError("", "OTP đã hết hạn.");
        //            model.Step = "EMAIL";
        //            return View(model);
        //        }

        //        if (attempts >= OtpMaxAttempts)
        //        {
        //            ModelState.AddModelError("", "Quá số lần sai. Gửi lại OTP.");
        //            model.Step = "EMAIL";
        //            return View(model);
        //        }

        //        if (otpStored != model.Otp)
        //        {
        //            await _http.PatchAsync($"/pendingRegistrations/{key}.json",
        //                new StringContent(JsonConvert.SerializeObject(new { attempts = attempts + 1 }),
        //                Encoding.UTF8, "application/json"));
        //            ModelState.AddModelError("Otp", "OTP không đúng.");
        //            model.Step = "OTP";
        //            return View(model);
        //        }

        //        TempData["Info"] = "OTP hợp lệ. Nhập thông tin tài khoản.";
        //        model.Step = "DETAILS";
        //        return View(model);
        //    }

        //    // --- STEP 3: Nhập thông tin & tạo tài khoản ---
        //    if (model.Step == "DETAILS" && actionType == "FINAL")
        //    {
        //        // Validate details
        //        ValidateDetails(model);
        //        if (!ModelState.IsValid)
        //        {
        //            model.Step = "DETAILS";
        //            return View(model);
        //        }

        //        // Re-check pending (đảm bảo chưa hết hạn & chưa bị lợi dụng)
        //        var key = EmailKey(model.Email);
        //        var pendingResp = await _http.GetAsync($"/pendingRegistrations/{key}.json");
        //        var body = await pendingResp.Content.ReadAsStringAsync();
        //        if (body == "null")
        //        {
        //            ModelState.Clear();
        //            ModelState.AddModelError("", "Phiên OTP hết hạn. Bắt đầu lại.");
        //            model.Step = "EMAIL";
        //            return View(model);
        //        }

        //        // Tạo Firebase Auth user
        //        var signupPayload = new { email = model.Email, password = model.Password, returnSecureToken = true };
        //        var signupResp = await _http.PostAsync($"{FirebaseAuthUrl}signUp?key={FirebaseApiKey}",
        //            new StringContent(JsonConvert.SerializeObject(signupPayload), Encoding.UTF8, "application/json"));
        //        var signupBody = await signupResp.Content.ReadAsStringAsync();
        //        if (!signupResp.IsSuccessStatusCode)
        //        {
        //            ModelState.Clear();
        //            ModelState.AddModelError("", "Không tạo được tài khoản: " + MapFirebaseSignUpError(signupBody));
        //            model.Step = "DETAILS";
        //            return View(model);
        //        }

        //        dynamic signup = JsonConvert.DeserializeObject(signupBody);
        //        string userId = signup.localId;

        //        var userData = new
        //        {
        //            userId,
        //            hoTen = model.HoTen,
        //            email = model.Email,
        //            userRole = "User",
        //            soDienThoai = model.SoDienThoai,
        //            diaChi = model.DiaChi,
        //            tenDangNhap = model.TenDangNhap,
        //            isVerified = true,
        //            createdAt = DateTime.UtcNow.ToString("o")
        //        };
        //        await _http.PutAsync($"/users/{userId}.json",
        //            new StringContent(JsonConvert.SerializeObject(userData), Encoding.UTF8, "application/json"));

        //        await _http.DeleteAsync($"/pendingRegistrations/{key}.json");

        //        TempData["Success"] = "Đăng ký thành công";
        //        return RedirectToAction("Login");
        //    }

        //    // Fallback
        //    model.Step = "EMAIL";
        //    return View(model);
        //}

        private async Task<bool> EmailExistsAsync(string email)
        {
            var payload = new { identifier = email, continueUri = "https://example.com" };
            var res = await _http.PostAsync(
                $"{FirebaseAuthUrl}createAuthUri?key={FirebaseApiKey}",
                new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"));
            if (!res.IsSuccessStatusCode) return false;
            dynamic data = JsonConvert.DeserializeObject(await res.Content.ReadAsStringAsync());
            try { return data.signInMethods != null && data.signInMethods.Count > 0; } catch { return false; }
        }

        private void ValidateDetails(RegisterModel m)
        {
            if (string.IsNullOrWhiteSpace(m.HoTen))
                ModelState.AddModelError("HoTen", "Họ tên không được để trống");
            if (string.IsNullOrWhiteSpace(m.TenDangNhap))
                ModelState.AddModelError("TenDangNhap", "Tên đăng nhập không được để trống");
            if (string.IsNullOrWhiteSpace(m.Password) || m.Password.Length < 8)
                ModelState.AddModelError("Password", "Mật khẩu phải ≥ 8 ký tự");
            if (m.Password != m.ConfirmPassword)
                ModelState.AddModelError("ConfirmPassword", "Mật khẩu xác nhận không khớp");
        }
        [AllowAnonymous]
        [HttpPost]
        public async Task<JsonResult> VerifyOtpAjax(string email, string otp)
        {
            email = email?.Trim().ToLowerInvariant();
            otp = otp?.Trim();
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(otp))
                return Json(new { success = false, message = "Thiếu email hoặc OTP." });

            if (otp.Length != OtpLength)
                return Json(new { success = false, message = "OTP phải gồm 6 số." });

            var key = EmailKey(email);
            var resp = await _http.GetAsync($"/pendingRegistrations/{key}.json");
            var body = await resp.Content.ReadAsStringAsync();
            if (body == "null")
                return Json(new { success = false, message = "Không tìm thấy hoặc OTP hết hạn." });

            dynamic pend = JsonConvert.DeserializeObject(body);
            long expiresAt = pend.expiresAt;
            int attempts = pend.attempts;
            string stored = pend.otp;
            bool verified = pend.verified ?? false;

            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (nowMs > expiresAt)
            {
                await _http.DeleteAsync($"/pendingRegistrations/{key}.json");
                return Json(new { success = false, message = "OTP đã hết hạn. Hãy gửi lại." });
            }

            if (verified)
                return Json(new { success = true, message = "OTP đã xác thực trước đó." });

            if (attempts >= OtpMaxAttempts)
                return Json(new { success = false, message = "Nhập sai quá số lần cho phép." });

            if (stored != otp)
            {
                var patchWrong = new { attempts = attempts + 1 };
                await _http.PatchAsync($"/pendingRegistrations/{key}.json",
                    new StringContent(JsonConvert.SerializeObject(patchWrong), Encoding.UTF8, "application/json"));
                return Json(new { success = false, message = "OTP không đúng." });
            }

            // Đúng
            var patchOk = new { verified = true, attempts = attempts };
            await _http.PatchAsync($"/pendingRegistrations/{key}.json",
                new StringContent(JsonConvert.SerializeObject(patchOk), Encoding.UTF8, "application/json"));

            return Json(new { success = true, message = "OTP hợp lệ." });
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult VerifyOtp(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return RedirectToAction("Register");
            return View(new VerifyOtpModel { Email = email });
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyOtp(VerifyOtpModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var email = model.Email?.Trim().ToLowerInvariant();
            var key = EmailKey(email);

            try
            {
                var pendingResp = await _http.GetAsync($"/pendingRegistrations/{key}.json");
                var pendingBody = await pendingResp.Content.ReadAsStringAsync();

                if (!pendingResp.IsSuccessStatusCode || pendingBody == "null")
                {
                    TempData["Error"] = "Không tìm thấy yêu cầu đăng ký hoặc đã hết hạn.";
                    return RedirectToAction("Register");
                }

                dynamic pending = JsonConvert.DeserializeObject(pendingBody);
                string otpStored = pending.otp;
                long expiresAtMs = pending.expiresAt;
                int attempts = pending.attempts;

                var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                if (nowMs > expiresAtMs)
                {
                    TempData["Error"] = "OTP đã hết hạn. Vui lòng đăng ký lại.";
                    return RedirectToAction("Register");
                }

                if (attempts >= OtpMaxAttempts)
                {
                    TempData["Error"] = "Quá số lần nhập sai. Vui lòng gửi lại OTP.";
                    return RedirectToAction("VerifyOtp", new { email });
                }

                if (otpStored != model.Otp)
                {
                    var patchAttempts = new { attempts = attempts + 1 };
                    await _http.PatchAsync($"/pendingRegistrations/{key}.json",
                        new StringContent(JsonConvert.SerializeObject(patchAttempts), Encoding.UTF8, "application/json"));
                    TempData["Error"] = "OTP không đúng.";
                    return RedirectToAction("VerifyOtp", new { email });
                }

                // ---- OTP hợp lệ → Tạo user ----
                string password = pending.data.password;
                string hoTen = pending.data.hoTen;
                string soDienThoai = pending.data.soDienThoai;
                string diaChi = pending.data.diaChi;
                string tenDangNhap = pending.data.tenDangNhap;

                var authPayload = new { email, password, returnSecureToken = true };
                var authContent = new StringContent(JsonConvert.SerializeObject(authPayload), Encoding.UTF8, "application/json");

                var authResp = await _http.PostAsync($"{FirebaseAuthUrl}signUp?key={FirebaseApiKey}", authContent);
                var authBody = await authResp.Content.ReadAsStringAsync();
                if (!authResp.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Không tạo được tài khoản: " + MapFirebaseSignUpError(authBody);
                    return RedirectToAction("VerifyOtp", new { email });
                }

                dynamic authResult = JsonConvert.DeserializeObject(authBody);
                string userId = authResult.localId;

                var userData = new
                {
                    userId,
                    hoTen,
                    email,
                    userRole = "User",
                    soDienThoai,
                    diaChi,
                    tenDangNhap,
                    isVerified = true,
                    createdAt = DateTime.UtcNow.ToString("o")
                };

                await _http.PutAsync($"/users/{userId}.json",
                    new StringContent(JsonConvert.SerializeObject(userData), Encoding.UTF8, "application/json"));

                await _http.DeleteAsync($"/pendingRegistrations/{key}.json");

                TempData["Success"] = "Xác thực thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
                return RedirectToAction("VerifyOtp", new { email });
            }
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<JsonResult> ResendOtp(string email)
        {
            email = email?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(email))
                return Json(new { success = false, message = "Email không hợp lệ." });

            // Không cho resend nếu đã verified (tránh reset)
            var key = EmailKey(email);
            var resp = await _http.GetAsync($"/pendingRegistrations/{key}.json");
            var body = await resp.Content.ReadAsStringAsync();
            if (body == "null")
                return Json(new { success = false, message = "Phiên OTP không tồn tại hoặc hết hạn." });

            dynamic pend = JsonConvert.DeserializeObject(body);
            long expiresAt = pend.expiresAt;
            long lastSent = pend.lastSent;
            int resendCount = pend.resendCount ?? 0;
            bool verified = pend.verified ?? false;

            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (nowMs > expiresAt)
            {
                await _http.DeleteAsync($"/pendingRegistrations/{key}.json");
                return Json(new { success = false, message = "OTP đã hết hạn. Gửi lại từ đầu." });
            }

            if (verified)
                return Json(new { success = false, message = "Email đã xác thực OTP." });

            if (nowMs - lastSent < OtpResendCooldownSeconds * 1000)
            {
                int remain = (int)((OtpResendCooldownSeconds * 1000 - (nowMs - lastSent)) / 1000);
                return Json(new { success = false, message = $"Vui lòng chờ {remain}s.", cooldown = remain });
            }

            string newOtp = GenerateOtp();
            var patch = new
            {
                otp = newOtp,
                lastSent = nowMs,
                resendCount = resendCount + 1,
                attempts = 0
            };

            await _http.PatchAsync($"/pendingRegistrations/{key}.json",
                new StringContent(JsonConvert.SerializeObject(patch), Encoding.UTF8, "application/json"));

            try
            {
                await _otpSender.SendOtpAsync(email, newOtp);
                return Json(new { success = true, message = "Đã gửi OTP mới.", cooldown = OtpResendCooldownSeconds });
            }
            catch
            {
                return Json(new { success = false, message = "Không gửi được OTP mới." });
            }
        }
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> CompleteRegistration(RegisterModel model)
        {
            model.Email = model.Email?.Trim().ToLowerInvariant();
            if (model.Email == null || !new EmailAddressAttribute().IsValid(model.Email))
                return Json(new { success = false, message = "Email không hợp lệ." });

            if (!TryValidateModel(model))
            {
                var errs = new List<string>();
                foreach (var kv in ModelState)
                    foreach (var e in kv.Value.Errors) errs.Add(e.ErrorMessage);
                return Json(new { success = false, message = string.Join("; ", errs) });
            }

            var key = EmailKey(model.Email);
            var pendingResp = await _http.GetAsync($"/pendingRegistrations/{key}.json");
            var pendingBody = await pendingResp.Content.ReadAsStringAsync();
            if (pendingBody == "null")
                return Json(new { success = false, message = "OTP chưa được gửi hoặc đã hết hạn." });

            dynamic pend = JsonConvert.DeserializeObject(pendingBody);
            bool verified = pend.verified ?? false;
            long expiresAt = pend.expiresAt;
            if (!verified)
                return Json(new { success = false, message = "Bạn chưa xác thực OTP." });

            if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > expiresAt)
                return Json(new { success = false, message = "OTP đã hết hạn. Gửi lại." });

            if (await EmailExistsAsync(model.Email))
                return Json(new { success = false, message = "Email đã được đăng ký (race condition)." });

            var signupPayload = new
            {
                email = model.Email,
                password = model.Password,
                returnSecureToken = true
            };
            var signupResp = await _http.PostAsync($"{FirebaseAuthUrl}signUp?key={FirebaseApiKey}",
                new StringContent(JsonConvert.SerializeObject(signupPayload), Encoding.UTF8, "application/json"));
            var signupBody = await signupResp.Content.ReadAsStringAsync();
            if (!signupResp.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = "Không tạo được tài khoản: " + MapFirebaseSignUpError(signupBody) });
            }

            dynamic signup = JsonConvert.DeserializeObject(signupBody);
            string userId = signup.localId;

            // 4. Ghi hồ sơ user vào Realtime DB
            var userData = new
            {
                userId,
                hoTen = model.HoTen,
                email = model.Email,
                userRole = "User",
                soDienThoai = model.SoDienThoai,
                diaChi = model.DiaChi,
                tenDangNhap = model.TenDangNhap,
                isVerified = true,
                createdAt = DateTime.UtcNow.ToString("o")
            };
            var putUser = await _http.PutAsync($"/users/{userId}.json",
                new StringContent(JsonConvert.SerializeObject(userData), Encoding.UTF8, "application/json"));

            if (!putUser.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = "Đăng ký thất bại khi ghi dữ liệu user." });
            }

            await _http.DeleteAsync($"/pendingRegistrations/{key}.json");

            await LogActivity(userId, "register", "Đăng ký thành công");

            return Json(new { success = true, message = "Đăng ký thành công. Bạn có thể đăng nhập." });
        }
        private string MapFirebaseSignUpError(string body)
        {
            try
            {
                dynamic o = JsonConvert.DeserializeObject(body);
                string code = o?.error?.message;
                switch (code)
                {
                    case "EMAIL_EXISTS": return "Email đã tồn tại.";
                    case "OPERATION_NOT_ALLOWED": return "Phương thức email/password đang bị vô hiệu hóa.";
                    case "TOO_MANY_ATTEMPTS_TRY_LATER": return "Quá nhiều lần thử. Thử lại sau.";
                    default: return code ?? "Không xác định";
                }
            }
            catch { return "Không xác định"; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            // Ghi log trước khi xóa session
            var userId = Session["UserId"]?.ToString();
            if (!string.IsNullOrEmpty(userId))
            {
                _ = LogActivity(userId, "logout", "Đăng xuất");
            }

            // Xóa session
            Session.Clear();
            Session.Abandon();

            // Xóa authentication cookie
            FormsAuthentication.SignOut();

            // Xóa cookie vật lý
            if (Request.Cookies[FormsAuthentication.FormsCookieName] != null)
            {
                var cookie = new HttpCookie(FormsAuthentication.FormsCookieName)
                {
                    Expires = DateTime.Now.AddYears(-1),
                    Path = "/"
                };
                Response.Cookies.Add(cookie);
            }

            // Xóa tất cả cookies khác
            foreach (string key in Request.Cookies.AllKeys)
            {
                Response.Cookies[key].Expires = DateTime.Now.AddDays(-1);
            }

            return RedirectToAction("Index", "Home");
        }
        [HttpGet]
        public ActionResult DeleteAccount()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteAccount(DeleteAccountModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                // 1. Xác thực lại để lấy idToken MỚI
                var verifyPayload = new
                {
                    email = Session["Email"]?.ToString(),
                    password = model.Password,
                    returnSecureToken = true
                };

                var verifyContent = new StringContent(JsonConvert.SerializeObject(verifyPayload),
                    Encoding.UTF8, "application/json");

                var verifyResponse = await _http.PostAsync(
                    $"{FirebaseAuthUrl}signInWithPassword?key={FirebaseApiKey}",
                    verifyContent);

                // Xử lý khi xác thực thất bại
                if (!verifyResponse.IsSuccessStatusCode)
                {
                    ModelState.AddModelError("Password", "Mật khẩu không chính xác");
                    return View(model);
                }

                // 2. Lấy idToken MỚI từ phản hồi
                var responseBody = await verifyResponse.Content.ReadAsStringAsync();
                dynamic authResult = JsonConvert.DeserializeObject(responseBody);
                string freshIdToken = authResult.idToken;

                // 3. Xóa tài khoản Firebase Authentication
                var deletePayload = new { idToken = freshIdToken };
                var deleteContent = new StringContent(JsonConvert.SerializeObject(deletePayload),
                    Encoding.UTF8, "application/json");

                var deleteResponse = await _http.PostAsync(
                    $"{FirebaseAuthUrl}delete?key={FirebaseApiKey}",
                    deleteContent);

                // Xử lý khi xóa thất bại
                if (!deleteResponse.IsSuccessStatusCode)
                {
                    var errorBody = await deleteResponse.Content.ReadAsStringAsync();
                    ModelState.AddModelError("", $"Xóa tài khoản thất bại: {errorBody}");
                    return View(model);
                }

                // 4. Xóa dữ liệu người dùng từ Realtime Database
                var userId = Session["UserId"]?.ToString();
                if (!string.IsNullOrEmpty(userId))
                {
                    await _http.DeleteAsync($"{FirebaseDbUrl}/users/{userId}.json?auth={freshIdToken}");
                }

                // 5. Đăng xuất và xóa phiên
                Session.Clear();
                FormsAuthentication.SignOut();

                // Xóa cookie vật lý
                Response.Cookies[FormsAuthentication.FormsCookieName].Expires = DateTime.Now.AddYears(-1);

                TempData["SuccessMessage"] = "Tài khoản đã được xóa thành công!";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Lỗi hệ thống: {ex.Message}");
                return View(model);
            }
        }


        private async Task LogActivity(string userId, string action, string details)
        {
            try
            {
                var activity = new
                {
                    timestamp = DateTime.UtcNow.ToString("o"),
                    action,
                    details
                };
                await _http.PostAsync($"/activity/{userId}.json",
                    new StringContent(JsonConvert.SerializeObject(activity), Encoding.UTF8, "application/json"));
            }
            catch { /* swallow */ }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditProfile(UserInfo model)
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login");

            if (!ModelState.IsValid)
                return View(model);

            var userId = Session["UserId"].ToString();
            var idToken = Session["FirebaseToken"]?.ToString();

            try
            {
                var updateData = new
                {
                    hoTen = model.HoTen,
                    soDienThoai = model.SoDienThoai,
                    diaChi = model.DiaChi,
                    tenDangNhap = model.TenDangNhap
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(updateData),
                    Encoding.UTF8,
                    "application/json"
                );

                var url = $"{FirebaseDbUrl}/users/{userId}.json";
                if (!string.IsNullOrEmpty(idToken))
                    url += $"?auth={idToken}";

                // Dùng HttpRequestMessage thay vì PatchAsync extension (chắc chắn hoạt động)
                var request = new HttpRequestMessage(new HttpMethod("PATCH"), url)
                {
                    Content = content
                };

                var response = await _http.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    Session["FullName"] = model.HoTen;
                    Session["TenDangNhap"] = model.TenDangNhap;
                    TempData["Message"] = "Cập nhật thông tin thành công!";
                }
                else
                {
                    var body = await response.Content.ReadAsStringAsync();
                    TempData["Error"] = $"Cập nhật thất bại: {(int)response.StatusCode} - {response.ReasonPhrase} | {body}";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
            }

            return RedirectToAction("EditProfile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditProfileAjax(UserInfo model)
        {
            try
            {
                if (Session["UserId"] == null)
                    return Json(new { success = false, message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại." });

                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
                }

                var userId = Session["UserId"].ToString();
                var idToken = Session["FirebaseToken"]?.ToString();

                // Thử cập nhật với token hiện tại
                var result = await TryUpdateProfile(userId, idToken, model);
                if (result.success) return Json(result);

                // Nếu token hết hạn, thử làm mới token
                var refreshResult = await RefreshFirebaseToken();
                if (!refreshResult.success)
                    return Json(new { success = false, message = "Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại." });

                // Thử cập nhật lại với token mới
                return Json(await TryUpdateProfile(userId, Session["FirebaseToken"].ToString(), model));
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        private async Task<dynamic> TryUpdateProfile(string userId, string idToken, UserInfo model)
        {
            try
            {
                var updateData = new
                {
                    hoTen = model.HoTen?.Trim(),
                    soDienThoai = model.SoDienThoai?.Trim(),
                    diaChi = model.DiaChi?.Trim(),
                    tenDangNhap = model.TenDangNhap?.Trim()
                };

                var json = JsonConvert.SerializeObject(updateData);
                var url = $"{FirebaseDbUrl}/users/{userId}.json?auth={idToken}";

                var response = await _http.PatchAsync(url,
                    new StringContent(json, Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode)
                {
                    return new
                    {
                        success = false,
                        message = $"Cập nhật thất bại: {response.StatusCode}"
                    };
                }

                // Cập nhật session
                Session["FullName"] = updateData.hoTen;
                Session["TenDangNhap"] = updateData.tenDangNhap;

                return new { success = true, message = "Cập nhật thành công!" };
            }
            catch
            {
                return new { success = false, message = "Lỗi khi cập nhật" };
            }
        }

        private async Task<dynamic> RefreshFirebaseToken()
        {
            try
            {
                var refreshToken = Session["RefreshToken"]?.ToString();
                if (string.IsNullOrEmpty(refreshToken))
                    return new { success = false };

                var payload = new
                {
                    grant_type = "refresh_token",
                    refresh_token = refreshToken
                };

                var response = await _http.PostAsync(
                    $"https://securetoken.googleapis.com/v1/token?key={FirebaseApiKey}",
                    new StringContent(JsonConvert.SerializeObject(payload),
                    Encoding.UTF8,
                    "application/json"));

                if (!response.IsSuccessStatusCode)
                    return new { success = false };

                var result = JsonConvert.DeserializeObject<dynamic>(
                    await response.Content.ReadAsStringAsync());

                Session["FirebaseToken"] = result.id_token;
                Session["RefreshToken"] = result.refresh_token;

                return new { success = true };
            }
            catch
            {
                return new { success = false };
            }
        }
        [HttpGet]
        public ActionResult ChangePassword()
        {
            return View(new ChangePasswordModel
            {
                Email = Session["Email"]?.ToString()
            });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordModel model)
        {
            try
            {
                // Kiểm tra phiên
                if (Session["Email"] == null)
                {
                    TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login");
                }

                model.Email = Session["Email"].ToString();

                // Xác thực mật khẩu cũ
                var verifyResult = await VerifyPassword(model.Email, model.OldPassword);
                if (!verifyResult.success)
                {
                    TempData["ErrorMessage"] = verifyResult.message;
                    return RedirectToAction("ChangePassword");
                }

                // Cập nhật mật khẩu mới
                var changeResult = await UpdatePassword(verifyResult.idToken, model.NewPassword);
                if (!changeResult.success)
                {
                    TempData["ErrorMessage"] = changeResult.message;
                    return RedirectToAction("ChangePassword");
                }

                // Cập nhật token mới
                Session["FirebaseToken"] = changeResult.newIdToken;
                TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
                return RedirectToAction("ChangePassword");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi hệ thống: " + ex.Message;
                return RedirectToAction("ChangePassword");
            }
        }

        private async Task<dynamic> VerifyPassword(string email, string password)
        {
            var payload = new { email, password, returnSecureToken = true };
            var response = await _http.PostAsync(
                $"{FirebaseAuthUrl}signInWithPassword?key={FirebaseApiKey}",
                new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                return new { success = false, message = "Mật khẩu hiện tại không đúng." };
            }

            var result = JsonConvert.DeserializeObject<dynamic>(
                await response.Content.ReadAsStringAsync());

            return new
            {
                success = true,
                idToken = (string)result.idToken
            };
        }

        private async Task<dynamic> UpdatePassword(string idToken, string newPassword)
        {
            var payload = new { idToken, password = newPassword, returnSecureToken = true };
            var response = await _http.PostAsync(
                $"{FirebaseAuthUrl}update?key={FirebaseApiKey}",
                new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                return new
                {
                    success = false,
                    message = "Đổi mật khẩu thất bại: " + MapFirebasePasswordError(await response.Content.ReadAsStringAsync())
                };
            }

            var result = JsonConvert.DeserializeObject<dynamic>(
                await response.Content.ReadAsStringAsync());

            return new
            {
                success = true,
                newIdToken = (string)result.idToken
            };
        }

        // Helper map lỗi Firebase
        private string MapFirebasePasswordError(string rawJson)
        {
            try
            {
                dynamic err = JsonConvert.DeserializeObject(rawJson);
                string code = err?.error?.message;

                if (string.IsNullOrEmpty(code))
                    return "Không xác định";

                switch (code)
                {
                    case "WEAK_PASSWORD":
                        return "Mật khẩu quá yếu.";
                    case "OPERATION_NOT_ALLOWED":
                        return "Tính năng đổi mật khẩu đang bị tắt.";
                    case "INVALID_ID_TOKEN":
                        return "Token không hợp lệ, đăng nhập lại.";
                    case "USER_DISABLED":
                        return "Tài khoản đã bị vô hiệu hóa.";
                    case "TOO_MANY_ATTEMPTS_TRY_LATER":
                        return "Thử quá nhiều lần. Vui lòng thử lại sau.";
                    case "TOKEN_EXPIRED":
                    case "USER_TOKEN_EXPIRED":
                        return "Phiên đăng nhập đã hết hạn, hãy đăng nhập lại.";
                    default:
                        return code; // trả lại mã gốc nếu chưa map
                }
            }
            catch
            {
                return "Không xác định";
            }
        }





    }
}