using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace SneakerSportStore.Models
{
    public class CheckoutViewModel
    {
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public List<CartItem> SelectedCartItems { get; set; }
        //public string SelectedVoucherCode { get; set; }
        public List<DiscountCode> SavedVouchers { get; set; }
        public double TotalBeforeDiscount { get; set; }
        public double DiscountAmount { get; set; }
        public double TotalAfterDiscount { get; set; }
        //public decimal TongTienCuoi { get; set; }
        public string PaymentMethod { get; set; }
        //public string CustomerName { get; set; }   
        //public string ProductId { get; set; }   

        //public string PhoneNumber { get; set; }    
        //public string SelectedVoucherId { get; set; }

        public string QRCodeBase64 { get; set; }
        public DiscountCode AppliedVoucher { get; set; }
        public string BankTransferScreenshot { get; set; }// hình ảnh 
        [Display(Name = "Ảnh chuyển khoản")]
        public HttpPostedFileBase BankTransferImage { get; set; }
        //public string AppliedVoucherCode { get; set; }
        //public double VoucherDiscount { get; set; }
        //public bool IsVoucherApplied { get; set; }

    }

}
