using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SneakerSportStore.Models
{
    public class CheckoutViewModel
    {
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public List<CartItem> SelectedCartItems { get; set; }
        public string SelectedVoucherCode { get; set; }
        public List<DiscountCode> SavedVouchers { get; set; }
        public double TotalBeforeDiscount { get; set; }
        public double DiscountAmount { get; set; }
        public double TotalAfterDiscount { get; set; }
        public string PaymentMethod { get; set; }
        public string CustomerName { get; set; }   
        public string PhoneNumber { get; set; }    
        public string SelectedVoucherId { get; set; } 

    }
}
