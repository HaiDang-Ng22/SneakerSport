using SneakerSportStore.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace SneakerSportStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly string FirebaseDbUrl = "https://sneakersportstore-default-rtdb.asia-southeast1.firebasedatabase.app/";

        // GET: /
        public async Task<ActionResult> Index(string searchTerm = "")
        {
            List<Giay> products = new List<Giay>();

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(FirebaseDbUrl + "/products.json");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, Giay>>(json);

                    if (dict != null)
                    {
                        foreach (var item in dict)
                        {
                            item.Value.FirebaseKey = item.Key;
                            products.Add(item.Value);
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                products = products.Where(g => g.TenGiay != null && g.TenGiay.ToLower().Contains(searchTerm.ToLower())).ToList();
            }

            ViewBag.SearchTerm = searchTerm;
            return View("SimpleIndex", products); // Chuyển hướng tới SimpleIndex.cshtml
        }

        // GET: /Home/Details/5
        public async Task<ActionResult> Details(string id)
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(FirebaseDbUrl + "/products/" + id + ".json");
                if (!response.IsSuccessStatusCode)
                {
                    return HttpNotFound("Sản phẩm không tồn tại.");
                }

                var json = await response.Content.ReadAsStringAsync();
                var product = JsonConvert.DeserializeObject<Giay>(json);
                ViewBag.ProductId = id;
                return View(product);
            }
        }

        // GET: /Home/Guide
        public ActionResult Guide()
        {
            return View();
        }

        // GET: /Home/About
        public ActionResult About()
        {
            return View();
        }
    }
}
