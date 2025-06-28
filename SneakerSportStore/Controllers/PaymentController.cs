using SneakerSportStore.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Mvc;

namespace SneakerSportStore.Controllers
{
    public class PaymentController : Controller
    {
        private Sneaker1Entities db = new Sneaker1Entities();

        // GET: Checkout
        public ActionResult Checkout(string productId)  // Use string for productId
        {
            if (string.IsNullOrEmpty(productId))
            {
                return HttpNotFound("Sản phẩm không hợp lệ.");
            }

            var product = db.Giays.Find(productId);  // Find the product using string ProductId
            if (product == null)
            {
                return HttpNotFound("Sản phẩm không tồn tại.");
            }

            // Pass product details to the view
            ViewBag.ProductName = product.TenGiay;
            ViewBag.ProductPrice = product.Gia;
            ViewBag.ProductImage = product.HinhAnh;
            ViewBag.ProductDescription = product.MoTa;

            // Define Payment Methods
            ViewBag.PaymentMethods = new List<SelectListItem>
            {
                new SelectListItem { Text = "Chuyển khoản", Value = "Transfer" },
                new SelectListItem { Text = "Thanh toán khi nhận hàng", Value = "COD" },
                new SelectListItem { Text = "Thẻ tín dụng / Ghi nợ", Value = "CreditDebit" },
                new SelectListItem { Text = "Momo", Value = "Momo" }
            };

            // Prepare Checkout ViewModel
            var model = new CheckoutViewModel
            {
                ProductId = productId,  // Pass productId as string
                ProductName = product.TenGiay,
                Price = product.Gia,
                TotalAmount = product.Gia  // Assuming the total amount is product price
            };

            return View(model);
        }

        // POST: Confirm Order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Confirm(CheckoutViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if ProductId is set
                if (string.IsNullOrEmpty(model.ProductId))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Product ID is missing.");
                }

                var product = db.Giays.Find(model.ProductId);

                if (product == null)
                {
                    return HttpNotFound("Sản phẩm không tồn tại.");
                }

                // Create new order
                var order = new DonDatHang
                {
                    NgayDat = DateTime.Now,
                    DiaChiGiaoHang = model.Address,
                    SoDienThoai = model.Phone,
                    Email = model.Email,
                    TongTien = model.TotalAmount,
                    PaymentMethod = model.PaymentMethod
                };

                // Add order to the database
                db.DonDatHangs.Add(order);
                db.SaveChanges();

                // Save success message and product details in TempData for confirmation
                TempData["SuccessMessage"] = "Đơn hàng của bạn đã được đặt thành công!";
                TempData["ProductName"] = product.TenGiay;
                TempData["TotalAmount"] = model.TotalAmount;
                TempData["ProductImage"] = product.HinhAnh;

                // Redirect to Order Confirmation
                return RedirectToAction("OrderConfirmation");
            }

            // Repopulate Payment Methods if ModelState is invalid
            ViewBag.PaymentMethods = new List<SelectListItem>
            {
                new SelectListItem { Text = "Chuyển khoản", Value = "Transfer" },
                new SelectListItem { Text = "Thanh toán khi nhận hàng", Value = "COD" },
                new SelectListItem { Text = "Thẻ tín dụng / Ghi nợ", Value = "CreditDebit" },
                new SelectListItem { Text = "Momo", Value = "Momo" }
            };

            return View("Checkout", model); // Return the same view if the model is invalid
        }

        // GET: Order Confirmation
        public ActionResult OrderConfirmation()
        {
            var model = new OrderConfirmationViewModel
            {
                SuccessMessage = TempData["SuccessMessage"] as string,
                ProductName = TempData["ProductName"] as string,
                TotalAmount = (decimal)(TempData["TotalAmount"] ?? 0),
                ProductImage = TempData["ProductImage"] as string
            };

            return View(model);
        }

    }
}
