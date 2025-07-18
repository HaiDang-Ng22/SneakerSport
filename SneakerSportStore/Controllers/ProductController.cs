using SneakerSportStore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Caching;

namespace SneakerSportStore.Controllers
{
    public class ProductController : Controller
    {
        private const string FirebaseDbUrl = "https://sneakersportstore-default-rtdb.asia-southeast1.firebasedatabase.app";

        private HttpClient GetFirebaseClient()
        {
            return new HttpClient { BaseAddress = new Uri(FirebaseDbUrl) };
        }

        public async Task<ActionResult> Index()
        {
            using (var client = GetFirebaseClient())
            {
                var response = await client.GetAsync("/products.json");
                if (!response.IsSuccessStatusCode)
                    return View(new List<ProductFireBaseKey>());

                var json = await response.Content.ReadAsStringAsync();
                var productsDict = JsonConvert.DeserializeObject<Dictionary<string, ProductFireBaseKey>>(json) ??
                                  new Dictionary<string, ProductFireBaseKey>();

                var products = productsDict.Select(kvp =>
                {
                    kvp.Value.GiayId = kvp.Key;
                    return kvp.Value;
                }).ToList();

                return View(products);
            }
        }

        [HttpGet]
        public async Task<ActionResult> Create()
        {
            await LoadSelectListsAsync();
            return View(new ProductFireBaseKey
            {
                NgaySanXuat = DateTime.Today,
                KichThuoc = new List<string>()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(ProductFireBaseKey model)
        {
            model.UpdateSizesFromString(Request.Form["KichThuocInput"]);
            model.UpdateAdditionalImagesFromString(Request.Form["AdditionalImagesInput"]);

            if (!model.IsValid(out var errorMessage))
            {
                ModelState.AddModelError("", errorMessage);
                await LoadSelectListsAsync();
                return View(model);
            }

            using (var client = GetFirebaseClient())
            {
                model.GiayId = Guid.NewGuid().ToString();
                var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");
                var response = await client.PutAsync($"/products/{model.GiayId}.json", content);

                if (response.IsSuccessStatusCode)
                {
                    HttpRuntime.Cache.Remove("NhaSanXuatList");
                    HttpRuntime.Cache.Remove("LoaiGiayList");

                    TempData["SuccessMessage"] = "Thêm sản phẩm thành công!";
                    return RedirectToAction("Index");
                }

                ModelState.AddModelError("", "Lỗi khi lưu sản phẩm vào Firebase");
            }

            await LoadSelectListsAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<ActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
                return HttpNotFound();

            using (var client = GetFirebaseClient())
            {
                var response = await client.GetAsync($"/products/{id}.json");
                if (!response.IsSuccessStatusCode)
                    return HttpNotFound();

                var product = JsonConvert.DeserializeObject<ProductFireBaseKey>(await response.Content.ReadAsStringAsync());
                if (product == null)
                    return HttpNotFound();

                product.GiayId = id;
                await LoadSelectListsAsync();
                return View(product);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(string id, ProductFireBaseKey model)
        {
            if (id != model.GiayId)
                return HttpNotFound();

            model.UpdateSizesFromString(Request.Form["KichThuocInput"]);
            model.UpdateAdditionalImagesFromString(Request.Form["AdditionalImagesInput"]);

            if (!model.IsValid(out var errorMessage))
            {
                ModelState.AddModelError("", errorMessage);
                await LoadSelectListsAsync();
                return View(model);
            }

            using (var client = GetFirebaseClient())
            {
                var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");
                var response = await client.PutAsync($"/products/{id}.json", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
                    return RedirectToAction("Index");
                }

                ModelState.AddModelError("", "Lỗi khi cập nhật Firebase");
            }

            await LoadSelectListsAsync();
            return View(model);
        }

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
                var product = JsonConvert.DeserializeObject<ProductFireBaseKey>(json);

                if (product == null)
                {
                    return HttpNotFound("Không tìm thấy sản phẩm.");
                }

                ViewBag.ProductId = id;
                return View(product);
            }
        }

        // POST: Product/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(string GiayId)
        {
            using (var client = new HttpClient())
            {
                var response = await client.DeleteAsync(FirebaseDbUrl + "/products/" + GiayId + ".json");

                await client.DeleteAsync(FirebaseDbUrl + "/comments/" + GiayId + ".json");

                if (response.IsSuccessStatusCode)
                {
                    TempData["Message"] = "Sản phẩm đã được xóa thành công!";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["Error"] = "Không thể xóa sản phẩm. Vui lòng thử lại!";
                    return RedirectToAction("Delete", new { id = GiayId });
                }
            }
        }


        [HttpPost]
        public async Task<JsonResult> AddManufacturer(string ten)
        {
            if (string.IsNullOrWhiteSpace(ten))
                return Json(new { success = false, message = "Tên không được trống" });

            using (var client = GetFirebaseClient())
            {
                var newId = $"NSX_{Guid.NewGuid().ToString("N")}";
                var nhaSanXuat = new NhaSanXuat
                {
                    NhaSanXuatID = newId,
                    TenNhaSanXuat = ten
                };

                var response = await client.PutAsync(
                    $"/nhasanxuat/{newId}.json",
                    new StringContent(JsonConvert.SerializeObject(nhaSanXuat), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    HttpRuntime.Cache.Remove("NhaSanXuatList");
                    return Json(new { success = true, id = newId, name = ten });
                }

                return Json(new { success = false, message = "Lỗi Firebase" });
            }
        }

        [HttpPost]
        public async Task<JsonResult> AddLoaiGiay(string ten)
        {
            if (string.IsNullOrWhiteSpace(ten))
                return Json(new { success = false, message = "Tên không được trống" });

            using (var client = GetFirebaseClient())
            {
                var newId = $"LG_{Guid.NewGuid().ToString("N")}";
                var loaiGiay = new LoaiGiay
                {
                    LoaiGiayID = newId,
                    TenLoaiGiay = ten
                };

                var response = await client.PutAsync(
                    $"/loaigiay/{newId}.json",
                    new StringContent(JsonConvert.SerializeObject(loaiGiay), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    HttpRuntime.Cache.Remove("LoaiGiayList");
                    return Json(new { success = true, id = newId, name = ten });
                }

                return Json(new { success = false, message = "Lỗi Firebase" });
            }
        }

        private async Task LoadSelectListsAsync()
        {
            var cache = HttpRuntime.Cache;
            List<NhaSanXuat> nhaSXList = null;
            List<LoaiGiay> loaiGiayList = null;

            if (cache["NhaSanXuatList"] == null)
            {
                using (var client = GetFirebaseClient())
                {
                    var response = await client.GetAsync("/nhasanxuat.json");
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var data = JsonConvert.DeserializeObject<Dictionary<string, NhaSanXuat>>(json);
                        nhaSXList = data?.Values.ToList() ?? new List<NhaSanXuat>();
                        cache.Insert("NhaSanXuatList", nhaSXList, null, DateTime.Now.AddMinutes(30), Cache.NoSlidingExpiration);
                    }
                }
            }
            else
            {
                nhaSXList = (List<NhaSanXuat>)cache["NhaSanXuatList"];
            }

            if (cache["LoaiGiayList"] == null)
            {
                using (var client = GetFirebaseClient())
                {
                    var response = await client.GetAsync("/loaigiay.json");
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var data = JsonConvert.DeserializeObject<Dictionary<string, LoaiGiay>>(json);
                        loaiGiayList = data?.Values.ToList() ?? new List<LoaiGiay>();
                        cache.Insert("LoaiGiayList", loaiGiayList, null, DateTime.Now.AddMinutes(30), Cache.NoSlidingExpiration);
                    }
                }
            }
            else
            {
                loaiGiayList = (List<LoaiGiay>)cache["LoaiGiayList"];
            }

            ViewBag.NhaSanXuatList = new SelectList(
                nhaSXList ?? new List<NhaSanXuat>(),
                "NhaSanXuatID",
                "TenNhaSanXuat");

            ViewBag.LoaiGiayList = new SelectList(
                loaiGiayList ?? new List<LoaiGiay>(),
                "LoaiGiayID",
                "TenLoaiGiay");
        }
    }
}