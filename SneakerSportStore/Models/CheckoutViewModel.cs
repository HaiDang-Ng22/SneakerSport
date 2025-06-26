using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SneakerSportStore.Models
{
    public class CheckoutViewModel
    {

        // Customer details
        [Required(ErrorMessage = "Tên không được để trống.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống.")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Địa chỉ không được để trống.")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Phương thức thanh toán không được để trống.")]
        public string PaymentMethod { get; set; }

        // Product and order details
        [Required(ErrorMessage = "Sản phẩm không hợp lệ.")]
        public string ProductId { get; set; }  // Product ID as string to match Firebase key

        [Required(ErrorMessage = "Số lượng không được để trống.")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0.")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Giá không được để trống.")]
        public decimal Price { get; set; }
        
        public decimal TotalAmount { get; set; }  // Total amount calculated

        public string BankAccountDetails { get; set; }

        // Product name, added to view model
        public string ProductName { get; set; }

        // List of available payment methods for dropdown
        public IEnumerable<System.Web.Mvc.SelectListItem> PaymentMethods { get; set; }
    }
}
