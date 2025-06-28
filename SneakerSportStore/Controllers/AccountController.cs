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
                    Session["CustomerID"] = userId; // ID người dùng (dùng cho mọi nghiệp vụ)
                    Session["UserId"] = userId;     // Gán thêm key này nếu bạn muốn dễ nhớ hơn

                    return RedirectToAction("Index", "Home");
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

            string role = "User"; // Tài khoản mới luôn là User

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
    }
}
