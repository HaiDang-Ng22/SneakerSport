using Newtonsoft.Json;
using SneakerSportStore.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
namespace SneakerSportStore.Models
{
    public class Order
    {
        public double DiscountAmount { get; set; }

        public string OrderId { get; set; }
        public string Status { get; set; } = "Chờ xử lý";
        public string UserId { get; set; }
        public string CustomerName { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public DateTime OrderDate { get; set; }
        public List<OrderItem> Items { get; set; }
        public double Total { get; set; }
        public string DiscountCode { get; set; }
        public double FinalTotal { get; set; }
        public double TotalAfterDiscount { get; set; }
        public string PaymentMethod { get; set; }
        [JsonProperty("bankTransferScreenshot")] // Thêm attribute này
        public string BankTransferScreenshot { get; set; }
        //public List<StatusHistory> StatusHistory { get; set; }
        public List<string> ReturnImages { get; set; } = new List<string>();
        public string ReturnStatus { get; set; }
        [JsonProperty("deliveryDate")]
        public DateTime? DeliveryDate { get; set; }
        public DateTime? ReturnRequestDate { get; set; } 
        public string ReturnReason { get; set; } 
        public string ProductImage { get; set; } 
        public string CancelReason { get; set; } 
        public DateTime? CancelDate { get; set; } 
    }

}
