using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using SneakerSportStore.Extensions;

namespace SneakerSportStore.Controllers
{
    public class SettingsController : Controller
    {
        private readonly string FirebaseDbUrl = "https://sneakersportstore-default-rtdb.asia-southeast1.firebasedatabase.app";
        private readonly string FirebaseApiKey = "AIzaSyCETbgg3mnPMIMha_KYFmFauleZwV6CSo4";

        // GET: Settings
        public async Task<ActionResult> Index()
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login", "Account");

            var userId = Session["UserID"].ToString();

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(FirebaseDbUrl + "/users/" + userId + ".json");
                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Không thể tải dữ liệu người dùng.";
                    return View();
                }

                var json = await response.Content.ReadAsStringAsync();
                var user = JsonConvert.DeserializeObject<dynamic>(json);
                return View(user);
            }
        }

        // POST: Settings/UpdateInfo
        [HttpPost]
        public async Task<ActionResult> UpdateInfo(string userId, string hoTen, string email, string soDienThoai, string diaChi)
        {
            using (var client = new HttpClient())
            {
                var updateData = new
                {
                    hoTen = hoTen,
                    email = email,
                    soDienThoai = soDienThoai,
                    diaChi = diaChi
                };

                var content = new StringContent(JsonConvert.SerializeObject(updateData), Encoding.UTF8, "application/json");
                var response = await client.PatchAsync(FirebaseDbUrl + "/users/" + userId + ".json", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Message"] = "Cập nhật thông tin thành công!";
                }
                else
                {
                    TempData["Error"] = "Không thể cập nhật thông tin.";
                }

                return RedirectToAction("Index");
            }
        }

        // POST: Settings/ChangePassword
        [HttpPost]
        public async Task<ActionResult> ChangePassword(string idToken, string newPassword)
        {
            using (var client = new HttpClient())
            {
                var data = new
                {
                    idToken = idToken,
                    password = newPassword,
                    returnSecureToken = true
                };

                var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://identitytoolkit.googleapis.com/v1/accounts:update?key=" + FirebaseApiKey, content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Message"] = "Đổi mật khẩu thành công.";
                }
                else
                {
                    TempData["Error"] = "Không thể đổi mật khẩu.";
                }

                return RedirectToAction("Index");
            }
        }
    }
}
