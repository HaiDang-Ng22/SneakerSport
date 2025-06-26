using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SneakerSportStore.Controllers
{
    public class FirebaseController : Controller
    {
        private readonly string firebaseUrl = "https://sneakersportstore-default-rtdb.asia-southeast1.firebasedatabase.app/";
        public async Task<ActionResult> SaveData()
        {
            var data = new { name = "Nguyen Van A", age = 25 };
            string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            string fullUrl = $"{firebaseUrl}test.json";

            using (var client = new HttpClient())
            {
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = await client.PutAsync(fullUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    ViewBag.Message = "Dữ liệu đã lưu thành công!";
                }
                else
                {
                    ViewBag.Message = "Lỗi khi lưu dữ liệu.";
                }
            }
            return View();
        }

        public async Task<ActionResult> GetData()
        {
            string fullUrl = $"{firebaseUrl}test.json";
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(fullUrl);
                if (response.IsSuccessStatusCode)
                {
                    string jsonData = await response.Content.ReadAsStringAsync();
                    ViewBag.Data = jsonData;
                }
                else
                {
                    ViewBag.Data = "Lỗi khi lấy dữ liệu.";
                }
            }
            return View();
        }
    }
}