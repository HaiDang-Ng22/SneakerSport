using SneakerSportStore.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using System.Text;
using System.Linq;


namespace SneakerSportStore.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly string FirebaseDbUrl = "https://sneakersportstore-default-rtdb.asia-southeast1.firebasedatabase.app";

        private async Task<List<CartItem>> GetCartItems(string userId)
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(FirebaseDbUrl + "/carts/" + userId + ".json");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var cartItems = JsonConvert.DeserializeObject<Dictionary<string, CartItem>>(json);
                    return cartItems?.Values.ToList() ?? new List<CartItem>();
                }
                return new List<CartItem>();
            }
        }


        public async Task<ActionResult> Checkout()
        {
            var userId = Session["UserId"]?.ToString();
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItems = await GetCartItems(userId); 
            return View(cartItems);
        }
    }

}