using SneakerSportStore.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
namespace SneakerSportStore.Models
{
    public class Order
    {

        public string OrderId { get; set; }
        public string Status { get; set; }
        public string UserId { get; set; }
        public string CustomerName { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public DateTime OrderDate { get; set; }
        public List<OrderItem> Items { get; set; }
        public double Total { get; set; }
        public string DiscountCode { get; set; }
        public double DiscountAmount { get; set; }
        public double FinalTotal { get; set; }
        public string PaymentMethod { get; set; }
    }

}
