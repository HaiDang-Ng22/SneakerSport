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

        // GET: Login
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        // POST: Login
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

                    // Lấy thông tin người dùng từ Realtime Database
                    string userId = result.localId;
                    var userInfoResponse = await client.GetAsync($"{FirebaseDbUrl}/users/{userId}.json");
                    var userInfo = await userInfoResponse.Content.ReadAsStringAsync();
                    dynamic userData = JsonConvert.DeserializeObject(userInfo);

                    // Lưu session
                    Session["FirebaseToken"] = result.idToken;
                    Session["Username"] = result.email;
                    Session["UserRole"] = userData?.userRole ?? "User";
                    Session["FullName"] = userData?.hoTen ?? "";
                    Session["Email"] = result.email;
                    Session["CustomerID"] = userId; // Lưu customerId để nhận diện người dùng

                    return RedirectToAction("Index", "Home");  // Redirect đến trang chủ hoặc trang giỏ hàng
                }

                ViewBag.ErrorMessage = "Tên đăng nhập hoặc mật khẩu không chính xác.";
                return View();
            }
        }

        // GET: Register
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

            string role = "User"; // Mặc định tất cả tài khoản là User

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
                    var firebaseUser = new
                    {
                        hoTen = newUser.HoTen,
                        email = newUser.Email,
                        userRole = role,
                        soDienThoai = newUser.SoDienThoai,
                        diaChi = newUser.DiaChi,
                        tenDangNhap = newUser.TenDangNhap
                    };

                    var userId = JsonConvert.DeserializeObject<dynamic>(responseBody).localId;
                    await client.PutAsync(
                        $"https://sneakersportstore-default-rtdb.asia-southeast1.firebasedatabase.app/users/{userId}.json",
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
            Session.Clear();  // Xóa tất cả session khi đăng xuất
            TempData["SuccessMessage"] = "Bạn đã đăng xuất thành công.";
            return RedirectToAction("Login");
        }
    }
}
