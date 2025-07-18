using SneakerSportStore.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;

namespace SneakerSportStore.Controllers
{
    public class CartController : Controller
    {
        private readonly string FirebaseDbUrl = "https://sneakersportstore-default-rtdb.asia-southeast1.firebasedatabase.app";

        [HttpPost]
        public async Task<ActionResult> ProceedToCheckout(List<string> selectedProductIds)
        {
            var userId = Session["UserId"]?.ToString();
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var cartItems = await GetCartItems(userId);
            var validItems = new List<CartItem>();
            var invalidItems = new List<string>();

            // Lọc các sản phẩm được chọn
            var selectedCartItems = cartItems
                .Where(x => selectedProductIds.Contains(x.ProductId))
                .ToList();

            if (!selectedCartItems.Any())
            {
                TempData["ErrorMessage"] = "Vui lòng chọn sản phẩm để thanh toán!";
                return RedirectToAction("Index");
            }

            // Kiểm tra tồn kho song song
            using (var client = new HttpClient())
            {
                var productCache = new ConcurrentDictionary<string, ProductFireBaseKey>();
                var tasks = new List<Task>();

                foreach (var item in selectedCartItems)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        var response = await client.GetAsync($"{FirebaseDbUrl}/products/{item.ProductId}.json");
                        if (response.IsSuccessStatusCode)
                        {
                            var json = await response.Content.ReadAsStringAsync();
                            var product = JsonConvert.DeserializeObject<ProductFireBaseKey>(json);
                            productCache.TryAdd(item.ProductId, product);
                        }
                    }));
                }

                await Task.WhenAll(tasks);

                // Xác thực số lượng tồn kho
                foreach (var item in selectedCartItems)
                {
                    if (productCache.TryGetValue(item.ProductId, out var product))
                    {
                        if (item.Quantity > product?.SoLuongTon)
                        {
                            invalidItems.Add(item.ProductName);
                        }
                        else
                        {
                            validItems.Add(item);
                        }
                    }
                    else
                    {
                        invalidItems.Add(item.ProductName);
                    }
                }
            }

            if (invalidItems.Any())
            {
                TempData["ErrorMessage"] = $"Sản phẩm vượt quá tồn kho: {string.Join(", ", invalidItems)}";
                return RedirectToAction("Index");
            }

            Session["SelectedCartItems"] = validItems;
            return RedirectToAction("Checkout", "Payment");
        }

        private async Task<List<CartItem>> GetCartItems(string userId)
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync($"{FirebaseDbUrl}/carts/{userId}.json");
                if (!response.IsSuccessStatusCode)
                    return new List<CartItem>();

                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json) || json == "null")
                    return new List<CartItem>();

                var cartDict = JsonConvert.DeserializeObject<Dictionary<string, CartItem>>(json);
                var cartItems = cartDict?.Values.ToList() ?? new List<CartItem>();

                // Cập nhật thông tin tồn kho mới nhất cho từng sản phẩm
                foreach (var item in cartItems)
                {
                    var productResponse = await client.GetAsync($"{FirebaseDbUrl}/products/{item.ProductId}.json");
                    if (productResponse.IsSuccessStatusCode)
                    {
                        var productJson = await productResponse.Content.ReadAsStringAsync();
                        var product = JsonConvert.DeserializeObject<ProductFireBaseKey>(productJson);
                        if (product != null)
                        {
                            // Cập nhật cả thông tin tồn kho và tên sản phẩm (phòng khi thay đổi)
                            item.SoluongTon = product.SoLuongTon;
                            item.ProductName = product.TenGiay;
                            item.Price = product.Gia;
                            item.ProductImage = product.HinhAnh;
                        }
                    }
                }

                return cartItems;
            }
        }

        private async Task SaveCartToFirebase(string userId, List<CartItem> cart)
        {
            using (var client = new HttpClient())
            {
                // Chuyển đổi sang dictionary với FirebaseKey làm key
                var cartDict = new Dictionary<string, CartItem>();
                foreach (var item in cart)
                {
                    cartDict[item.FirebaseKey] = item;
                }

                var jsonContent = JsonConvert.SerializeObject(cartDict);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                await client.PutAsync($"{FirebaseDbUrl}/carts/{userId}.json", content);
            }
        }

        [HttpPost]
        public async Task<ActionResult> AddToCart(string productId, int quantity)
        {
            var userId = Session["UserId"]?.ToString();
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync($"{FirebaseDbUrl}/products/{productId}.json");
                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] = "Sản phẩm không tồn tại";
                    return RedirectToAction("Index", "Product");
                }

                var json = await response.Content.ReadAsStringAsync();
                var product = JsonConvert.DeserializeObject<ProductFireBaseKey>(json);

                if (quantity > product.SoLuongTon)
                {
                    TempData["ErrorMessage"] = $"Số lượng vượt quá tồn kho (còn {product.SoLuongTon} sản phẩm)";
                    return RedirectToAction("Details", "Home", new { id = productId });
                }

                var cart = await GetCartItems(userId);

                var existingItem = cart.FirstOrDefault(c => c.ProductId == productId);

                if (existingItem != null)
                {
                    var newTotalQuantity = existingItem.Quantity + quantity;
                    if (newTotalQuantity > product.SoLuongTon)
                    {
                        TempData["ErrorMessage"] = $"Tổng số lượng vượt quá tồn kho (còn {product.SoLuongTon} sản phẩm)";
                        return RedirectToAction("Details", "Home", new { id = productId });
                    }

                    existingItem.Quantity = newTotalQuantity;
                }
                else
                {
                    cart.Add(new CartItem
                    {
                        FirebaseKey = Guid.NewGuid().ToString(),
                        ProductId = productId,
                        ProductName = product.TenGiay,
                        Price = product.Gia,
                        Quantity = quantity,
                        ProductImage = product.HinhAnh,
                        UserId = userId,
                        SoluongTon = product.SoLuongTon 
                    });
                }

                await SaveCartToFirebase(userId, cart);
                TempData["SuccessMessage"] = "Đã thêm vào giỏ hàng!";
                return RedirectToAction("Details", "Home", new { id = productId });
            }
        }

        [HttpPost]
        public async Task<ActionResult> RemoveFromCart(string firebaseKey)
        {
            var userId = Session["UserId"]?.ToString();
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            using (var client = new HttpClient())
            {
                var response = await client.DeleteAsync($"{FirebaseDbUrl}/carts/{userId}/{firebaseKey}.json");

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Đã xóa sản phẩm khỏi giỏ hàng!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Lỗi khi xóa sản phẩm";
                }

                return RedirectToAction("Index");
            }
        }

        public async Task<ActionResult> Index()
        {
            var userId = Session["UserId"]?.ToString();
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItems = await GetCartItems(userId);

            ViewBag.TotalPrice = cartItems.Sum(item => item.Price * item.Quantity);

            return View(cartItems);
        }

        //[HttpPost]
        //public async Task<ActionResult> UpdateQuantity(string firebaseKey, int newQuantity)
        //{
        //    var userId = Session["UserId"]?.ToString();
        //    if (string.IsNullOrEmpty(userId))
        //        return RedirectToAction("Login", "Account");

        //    if (newQuantity <= 0)
        //        return await RemoveFromCart(firebaseKey);

        //    var cart = await GetCartItems(userId);
        //    var item = cart.FirstOrDefault(i => i.FirebaseKey == firebaseKey);

        //    if (item == null)
        //    {
        //        TempData["ErrorMessage"] = "Không tìm thấy sản phẩm trong giỏ hàng";
        //        return RedirectToAction("Index");
        //    }

        //    using (var client = new HttpClient())
        //    {
        //        var response = await client.GetAsync($"{FirebaseDbUrl}/products/{item.ProductId}.json");
        //        if (response.IsSuccessStatusCode)
        //        {
        //            var json = await response.Content.ReadAsStringAsync();
        //            var product = JsonConvert.DeserializeObject<ProductFireBaseKey>(json);

        //            if (product == null)
        //            {
        //                TempData["ErrorMessage"] = "Sản phẩm không tồn tại trong hệ thống";
        //                return RedirectToAction("Index");
        //            }

        //            item.SoluongTon = product.SoLuongTon;

        //            if (newQuantity > product.SoLuongTon)
        //            {
        //                TempData["ErrorMessage"] = $"Số lượng vượt quá tồn kho (còn {product.SoLuongTon} sản phẩm)";
        //                return RedirectToAction("Index");
        //            }
        //        }
        //        else
        //        {
        //            TempData["ErrorMessage"] = "Không thể kiểm tra tồn kho sản phẩm";
        //            return RedirectToAction("Index");
        //        }
        //    }

        //    item.Quantity = newQuantity;
        //    await SaveCartToFirebase(userId, cart);

        //    TempData["SuccessMessage"] = "Đã cập nhật số lượng!";
        //    return RedirectToAction("Index");
        //}


        [HttpPost]
        public async Task<JsonResult> UpdateQuantity(string firebaseKey, int newQuantity)
        {
            try
            {
                var userId = Session["UserId"]?.ToString();
                if (string.IsNullOrEmpty(userId))
                    return Json(new { success = false, message = "Vui lòng đăng nhập" });

                var cart = await GetCartItems(userId);
                var item = cart.FirstOrDefault(i => i.FirebaseKey == firebaseKey);

                if (item == null)
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm" });

                // Kiểm tra tồn kho
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync($"{FirebaseDbUrl}/products/{item.ProductId}.json");
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var product = JsonConvert.DeserializeObject<ProductFireBaseKey>(json);

                        if (newQuantity > product?.SoLuongTon)
                            return Json(new { success = false, message = $"Số lượng vượt quá tồn kho (còn {product.SoLuongTon})" });
                    }
                }

                // Cập nhật số lượng
                item.Quantity = newQuantity;
                await SaveCartToFirebase(userId, cart);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}