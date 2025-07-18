using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SneakerSportStore.Models
{
    public class OrderItem
    {
        public int STT { get; set; }
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductImage { get; set; }
        public string ProductDescription { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total { get; set; }
        public decimal FinalTotal { get; set; }

    }
}