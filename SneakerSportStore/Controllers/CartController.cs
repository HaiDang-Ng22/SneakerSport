﻿using SneakerSportStore.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using System;

namespace SneakerSportStore.Controllers
{
    public class CartController : Controller
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

        private async Task SaveCartToFirebase(string userId, List<CartItem> cart)
        {
            using (var client = new HttpClient())
            {
                var jsonContent = JsonConvert.SerializeObject(cart.ToDictionary(c => c.FirebaseKey));
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await client.PutAsync(FirebaseDbUrl + "/carts/" + userId + ".json", content);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Không thể lưu giỏ hàng vào Firebase");
                }
            }
        }
        [HttpPost]
        public async Task<ActionResult> AddToCart(string productId, int quantity)
        {
            var userId = Session["CustomerID"]?.ToString();  // Lấy ID người dùng từ session

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");  // Chuyển đến trang đăng nhập nếu chưa đăng nhập
            }

            using (var client = new HttpClient())
            {
                var productResponse = await client.GetAsync(FirebaseDbUrl + "/products/" + productId + ".json");
                if (!productResponse.IsSuccessStatusCode)
                {
                    return HttpNotFound("Sản phẩm không tồn tại.");
                }

                var json = await productResponse.Content.ReadAsStringAsync();
                var product = JsonConvert.DeserializeObject<ProductFireBaseKey>(json);

                var cart = await GetCartItems(userId);

                var existingCartItem = cart.FirstOrDefault(c => c.FirebaseKey == productId);
                if (existingCartItem != null)
                {
                    existingCartItem.Quantity += quantity;
                }
                else
                {
                    var newCartItem = new CartItem
                    {
                        FirebaseKey = Guid.NewGuid().ToString(),
                        ProductName = product.TenGiay,
                        Price = product.Gia,
                        Quantity = quantity,
                        ProductImage = product.HinhAnh,
                        ProductDescription = product.MoTa,
                        UserId = userId
                    };
                    cart.Add(newCartItem);
                }

                await SaveCartToFirebase(userId, cart);
                return RedirectToAction("Index", "Product");
            }
        }


        public async Task<ActionResult> Index()
        {
            var userId = Session["CustomerID"]?.ToString();
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItems = await GetCartItems(userId);
            return View(cartItems);
        }
    }
}
