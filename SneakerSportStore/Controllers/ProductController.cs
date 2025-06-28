using SneakerSportStore.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using System.Linq;
using System;

namespace SneakerSportStore.Controllers
{
    public class ProductController : Controller
    {
        private readonly string FirebaseDbUrl = "https://sneakersportstore-default-rtdb.asia-southeast1.firebasedatabase.app";

        // GET: Product
        public async Task<ActionResult> Index()
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(FirebaseDbUrl + "/products.json");
                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Không thể tải dữ liệu sản phẩm.";
                    return View(new List<Giay>());
                }

                var json = await response.Content.ReadAsStringAsync();
                var dict = JsonConvert.DeserializeObject<Dictionary<string, Giay>>(json);

                var products = new List<Giay>();
                if (dict != null)
                {
                    foreach (var item in dict)
                    {
                        item.Value.GiayId = item.Key; // Assign FirebaseKey as the unique ID
                        products.Add(item.Value);
                    }
                }

                return View(products);
            }
        }

        // GET: Product/Create
        public async Task<ActionResult> Create()
        {
            // Lấy danh sách nhà sản xuất và loại giày từ Firebase (hoặc từ cơ sở dữ liệu)
            using (var client = new HttpClient())
            {
                var nhaSanXuatResponse = await client.GetAsync(FirebaseDbUrl + "/nhasanxuat.json");
                var loaiGiayResponse = await client.GetAsync(FirebaseDbUrl + "/loaigiay.json");

                if (nhaSanXuatResponse.IsSuccessStatusCode && loaiGiayResponse.IsSuccessStatusCode)
                {
                    var nhaSanXuatJson = await nhaSanXuatResponse.Content.ReadAsStringAsync();
                    var loaiGiayJson = await loaiGiayResponse.Content.ReadAsStringAsync();

                    var nhaSanXuatList = JsonConvert.DeserializeObject<Dictionary<string, NhaSanXuat>>(nhaSanXuatJson);
                    var loaiGiayList = JsonConvert.DeserializeObject<Dictionary<string, LoaiGiay>>(loaiGiayJson);

                    // Kiểm tra nếu các danh sách không null
                    if (nhaSanXuatList != null && loaiGiayList != null)
                    {
                        ViewBag.NhaSanXuatList = new SelectList(nhaSanXuatList.Values.ToList(), "NhaSanXuatID", "TenNhaSanXuat");
                        ViewBag.LoaiGiayList = new SelectList(loaiGiayList.Values.ToList(), "LoaiGiayID", "TenLoaiGiay");
                    }
                    else
                    {
                        ViewBag.Error = "Không thể tải danh sách nhà sản xuất hoặc loại giày.";
                    }
                }
                else
                {
                    ViewBag.Error = "Không thể tải danh sách nhà sản xuất hoặc loại giày.";
                }
            }

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(Giay model)
        {
            if (ModelState.IsValid)
            {
                using (var client = new HttpClient())
                {
                    // Tạo ID mới
                    model.GiayId = Guid.NewGuid().ToString();

                    // Gửi lên Firebase: dùng chính GiayId làm node
                    var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");
                    var response = await client.PutAsync(FirebaseDbUrl + $"/products/{model.GiayId}.json", content);

                    if (response.IsSuccessStatusCode)
                    {
                        TempData["Message"] = "Sản phẩm đã được thêm thành công!";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        string errorMessage = await response.Content.ReadAsStringAsync();
                        ViewBag.Error = $"Không thể thêm sản phẩm. Lỗi: {errorMessage}";
                    }
                }
            }
            return View(model);
        }


        // GET: Product/Edit/5
        // GET: Product/Edit/5
        public async Task<ActionResult> Edit(string id)
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(FirebaseDbUrl + "/products/" + id + ".json");
                if (!response.IsSuccessStatusCode)
                    return HttpNotFound("Không tìm thấy sản phẩm.");

                var json = await response.Content.ReadAsStringAsync();
                var product = JsonConvert.DeserializeObject<Giay>(json);
                ViewBag.ProductId = id;
                return View(product);
            }
        }


        // POST: Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(string id, Giay model)
        {
            if (ModelState.IsValid)
            {
                using (var client = new HttpClient())
                {
                    var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

                    var response = await client.PutAsync(FirebaseDbUrl + "/products/" + id + ".json", content);

                    if (response.IsSuccessStatusCode)
                    {
                        TempData["Message"] = "Sản phẩm đã được cập nhật thành công!";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        ViewBag.Error = "Không thể cập nhật sản phẩm.";
                    }
                }
            }

            return View(model);
        }

        // GET: Product/Delete/5
        public async Task<ActionResult> Delete(string id)
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

                if (product == null)
                {
                    return HttpNotFound("Không tìm thấy sản phẩm.");
                }

                ViewBag.ProductId = id;
                return View(product);
            }
        }

        // POST: Product/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(string id)
        {
            using (var client = new HttpClient())
            {
                var response = await client.DeleteAsync(FirebaseDbUrl + "/products/" + id + ".json");

                if (response.IsSuccessStatusCode)
                {
                    var userId = Session["CustomerID"]?.ToString();

                    if (!string.IsNullOrEmpty(userId))
                    {
                        var cart = await GetCartItems(userId);

                        var cartItem = cart.FirstOrDefault(c => c.FirebaseKey == id);
                        if (cartItem != null)
                        {
                            cart.Remove(cartItem);
                            await SaveCartToFirebase(userId, cart);
                        }
                    }

                    TempData["Message"] = "Sản phẩm đã được xóa thành công!";
                    return RedirectToAction("Index");
                }
                else
                {
                    ViewBag.Error = "Không thể xóa sản phẩm.";
                    return RedirectToAction("Delete", new { id });
                }
            }
        }

        // Method to get cart items from Firebase
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

        // Method to save cart items to Firebase
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
        // Lấy danh sách bình luận cho sản phẩm
        private async Task<List<Comment>> GetComments(string productId)
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(FirebaseDbUrl + $"/comments/{productId}.json");
                if (!response.IsSuccessStatusCode) return new List<Comment>();
                var json = await response.Content.ReadAsStringAsync();
                var dict = JsonConvert.DeserializeObject<Dictionary<string, Comment>>(json);
                return dict?.Values.OrderByDescending(x => x.CreatedAt).ToList() ?? new List<Comment>();
            }
        }

        // Thêm bình luận mới (hoặc trả lời)
        [HttpPost]
        public async Task<ActionResult> AddComment(string productId, string content, string parentCommentId = null)
        {
            var userId = Session["CustomerID"]?.ToString();
            var userName = Session["Username"]?.ToString();

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var comment = new Comment
            {
                CommentId = Guid.NewGuid().ToString(),
                ProductId = productId,
                UserId = userId,
                UserName = userName,
                Content = content,
                ParentCommentId = parentCommentId,
                CreatedAt = DateTime.Now
            };

            using (var client = new HttpClient())
            {
                var contentJson = new StringContent(JsonConvert.SerializeObject(comment), Encoding.UTF8, "application/json");
                await client.PutAsync(FirebaseDbUrl + $"/comments/{productId}/{comment.CommentId}.json", contentJson);
            }

            return RedirectToAction("Details", new { id = productId });
        }

        // Xóa bình luận (chỉ admin hoặc chủ bình luận)
        [HttpPost]
        public async Task<ActionResult> DeleteComment(string productId, string commentId)
        {
            var userId = Session["CustomerID"]?.ToString();
            var userRole = Session["UserRole"]?.ToString();

            // Lấy bình luận
            Comment comment = null;
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(FirebaseDbUrl + $"/comments/{productId}/{commentId}.json");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    comment = JsonConvert.DeserializeObject<Comment>(json);
                }
            }

            if (comment == null || (comment.UserId != userId && userRole != "Admin"))
                return new HttpStatusCodeResult(403, "Không có quyền xóa!");

            using (var client = new HttpClient())
            {
                await client.DeleteAsync(FirebaseDbUrl + $"/comments/{productId}/{commentId}.json");
            }

            return RedirectToAction("Details", new { id = productId });
        }

    }
}
