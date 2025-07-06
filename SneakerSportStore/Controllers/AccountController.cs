using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using SneakerSportStore.Models;

namespace SneakerSportStore.Controllers
{
    public class AccountController : Controller
    {
        private readonly string FirebaseApiKey = "AIzaSyCETbgg3mnPMIMha_KYFmFauleZwV6CSo4";
        private readonly string FirebaseDbUrl = "https://sneakersportstore-default-rtdb.asia-southeast1.firebasedatabase.app";

        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.ErrorMessage = "Tên đăng nhập và mật khẩu không được để trống.";
                return View();
            }

            var payload = new
            {
                email = username,
                password = password,
                returnSecureToken = true
            };

            using (var client = new HttpClient())
            {
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={FirebaseApiKey}", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    dynamic result = JsonConvert.DeserializeObject(responseBody);
                    string userId = result.localId;

                    // Lấy thông tin user từ Firebase
                    var userInfoResponse = await client.GetAsync($"{FirebaseDbUrl}/users/{userId}.json");
                    var userInfo = await userInfoResponse.Content.ReadAsStringAsync();
                    dynamic userData = JsonConvert.DeserializeObject(userInfo);

                    // Lưu vào session
                    Session["FirebaseToken"] = result.idToken;
                    Session["Username"] = result.email;
                    Session["UserRole"] = userData?.userRole ?? "User";
                    Session["FullName"] = userData?.hoTen ?? "";
                    Session["Email"] = result.email;
                    Session["CustomerID"] = userId; 
                    Session["UserId"] = userId;     

                    return RedirectToAction("Index", "Home");
                }

                ViewBag.ErrorMessage = "Tên đăng nhập hoặc mật khẩu không chính xác.";
                return View();
            }
        }

        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Register(KhachHang newUser)
        {
            if (string.IsNullOrWhiteSpace(newUser.Email) || string.IsNullOrWhiteSpace(newUser.MatKhau))
            {
                ViewBag.ErrorMessage = "Email và mật khẩu không được để trống.";
                return View(newUser);
            }

            string role = "User";

            var payload = new
            {
                email = newUser.Email,
                password = newUser.MatKhau,
                returnSecureToken = true
            };

            using (var client = new HttpClient())
            {
                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(
                    $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={FirebaseApiKey}",
                    content
                );

                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var userId = JsonConvert.DeserializeObject<dynamic>(responseBody).localId;

                    // Lưu user với id tự động vào Firebase
                    var firebaseUser = new
                    {
                        userId = userId, // <--- luôn có id
                        hoTen = newUser.HoTen,
                        email = newUser.Email,
                        userRole = role,
                        soDienThoai = newUser.SoDienThoai,
                        diaChi = newUser.DiaChi,
                        tenDangNhap = newUser.TenDangNhap
                    };

                    await client.PutAsync(
                        $"{FirebaseDbUrl}/users/{userId}.json",
                        new StringContent(JsonConvert.SerializeObject(firebaseUser), Encoding.UTF8, "application/json")
                    );

                    TempData["SuccessMessage"] = "Đăng ký thành công!";
                    return RedirectToAction("Login");
                }

                dynamic errorResult = JsonConvert.DeserializeObject(responseBody);
                ViewBag.ErrorMessage = $"Lỗi Firebase: {errorResult?.error?.message ?? "Không xác định"}";
                return View(newUser);
            }
        }

        public ActionResult Logout()
        {
            Session.Clear();
            TempData["SuccessMessage"] = "Bạn đã đăng xuất thành công.";
            return RedirectToAction("Login");
        }
        [HttpPost]
        public async Task<ActionResult> DeleteAccount()
        {
            var userId = Session["UserId"]?.ToString();
            var idToken = Session["FirebaseToken"]?.ToString();

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(idToken))
            {
                ViewBag.ErrorMessage = "Không tìm thấy thông tin người dùng. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login");
            }

            using (var client = new HttpClient())
            {
                // First, delete the user data from Firebase Realtime Database
                var dbDeleteResponse = await client.DeleteAsync($"{FirebaseDbUrl}/users/{userId}.json");

                if (!dbDeleteResponse.IsSuccessStatusCode)
                {
                    ViewBag.ErrorMessage = "Lỗi khi xóa dữ liệu người dùng từ Firebase Database.";
                    return View();
                }

                // Now, delete the user from Firebase Authentication
                var deletePayload = new
                {
                    idToken = idToken
                };

                var content = new StringContent(JsonConvert.SerializeObject(deletePayload), Encoding.UTF8, "application/json");
                var authDeleteResponse = await client.PostAsync($"https://identitytoolkit.googleapis.com/v1/accounts:delete?key={FirebaseApiKey}", content);
                var authDeleteResponseBody = await authDeleteResponse.Content.ReadAsStringAsync();

                if (authDeleteResponse.IsSuccessStatusCode)
                {
                    // Clear session and redirect to login
                    Session.Clear();
                    TempData["SuccessMessage"] = "Tài khoản đã được xóa thành công.";
                    return RedirectToAction("Login");
                }

                // Handle any error from Firebase Authentication
                dynamic errorResult = JsonConvert.DeserializeObject(authDeleteResponseBody);
                ViewBag.ErrorMessage = $"Lỗi Firebase: {errorResult?.error?.message ?? "Không xác định"}";
                return View();
            }
        }
    }
}

