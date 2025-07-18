//using System.Threading.Tasks;
//using System.Net.Http;
//using Newtonsoft.Json;
//using System.Text;
//using System.Web.Mvc;
//using SneakerSportStore.Models;
//using System;

//namespace SneakerSportStore.Controllers
//{
//    public class BankAccountController : Controller
//    {
//        private readonly string FirebaseDbUrl = "https://sneakersportstore-default-rtdb.asia-southeast1.firebasedatabase.app/";

//        // Phương thức để hiển thị form nhập thông tin tài khoản ngân hàng
//        public ActionResult BankAccountSetup()
//        {
//            return View();
//        }

//        // Phương thức để lưu thông tin tài khoản ngân hàng
//        [HttpPost]
//        public async Task<ActionResult> BankAccountSetup(BankAccountViewModel model)
//        {
//            var userId = Session["CustomerID"]?.ToString();
//            if (string.IsNullOrEmpty(userId))
//            {
//                TempData["ErrorMessage"] = "Vui lòng đăng nhập để liên kết tài khoản ngân hàng.";
//                return RedirectToAction("Login", "Account");
//            }

//            // Lưu thông tin tài khoản ngân hàng vào Firebase
//            var bankAccount = new UserBankAccount
//            {
//                UserId = userId,
//                BankName = model.BankName,
//                AccountNumber = model.AccountNumber,
//                AccountHolder = model.AccountHolder
//            };

//            var isSaved = await SaveBankAccountInfo(bankAccount);

//            if (isSaved)
//            {
//                TempData["SuccessMessage"] = "Thông tin tài khoản ngân hàng đã được lưu thành công.";
//                return RedirectToAction("BankAccountInfo", "BankAccount");
//            }
//            else
//            {
//                TempData["ErrorMessage"] = "Lỗi khi lưu thông tin tài khoản ngân hàng.";
//                return RedirectToAction("BankAccountSetup", "BankAccount");
//            }
//        }

//        // Phương thức để hiển thị thông tin tài khoản ngân hàng
//        public async Task<ActionResult> BankAccountInfo()
//        {
//            var userId = Session["CustomerID"]?.ToString();
//            if (string.IsNullOrEmpty(userId))
//            {
//                return RedirectToAction("Login", "Account");
//            }

//            var bankAccount = await GetUserBankAccount(userId);
//            if (bankAccount == null)
//            {
//                TempData["ErrorMessage"] = "Không tìm thấy thông tin tài khoản ngân hàng của bạn.";
//                return RedirectToAction("BankAccountSetup", "BankAccount");
//            }

//            return View(bankAccount);
//        }

//        // Phương thức để lưu thông tin tài khoản ngân hàng vào Firebase
//        private async Task<bool> SaveBankAccountInfo(UserBankAccount bankAccount)
//        {
//            using (var client = new HttpClient())
//            {
//                var json = JsonConvert.SerializeObject(bankAccount);
//                var content = new StringContent(json, Encoding.UTF8, "application/json");
//                var response = await client.PutAsync($"{FirebaseDbUrl}/userBankAccounts/{bankAccount.UserId}.json", content);

//                return response.IsSuccessStatusCode;
//            }
//        }

//        // Phương thức để lấy thông tin tài khoản ngân hàng của người dùng
//        private async Task<UserBankAccount> GetUserBankAccount(string userId)
//        {
//            UserBankAccount bankAccount = null;

//            using (var client = new HttpClient())
//            {
//                var response = await client.GetAsync($"{FirebaseDbUrl}/userBankAccounts/{userId}.json");

//                if (response.IsSuccessStatusCode)
//                {
//                    var json = await response.Content.ReadAsStringAsync();
//                    bankAccount = JsonConvert.DeserializeObject<UserBankAccount>(json);
//                }
//            }

//            return bankAccount;
//        }

//        // Phương thức để kết nối với ngân hàng và thực hiện thanh toán
//        private async Task<bool> ProcessPayment(string userId, double amount)
//        {
//            var userBankAccount = await GetUserBankAccount(userId);

//            if (userBankAccount != null)
//            {
//                return await CallBankApiToDeductAmount(userBankAccount, amount);
//            }

//            return false;
//        }

//        // Phương thức để gọi API ngân hàng và trừ tiền từ tài khoản người dùng
//        private async Task<bool> CallBankApiToDeductAmount(UserBankAccount bankAccount, double amount)
//        {
//            using (var client = new HttpClient())
//            {
//                var apiUrl = "https://bankapi.example.com/deduct"; // Thay bằng URL của ngân hàng
//                var response = await client.PostAsJsonAsync(apiUrl, new
//                {
//                    AccountNumber = bankAccount.AccountNumber,
//                    Amount = amount
//                });

//                return response.IsSuccessStatusCode;
//            }
//        }
//    }
//}
