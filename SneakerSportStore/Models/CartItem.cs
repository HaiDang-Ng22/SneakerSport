using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SneakerSportStore.Models;
namespace SneakerSportStore.Models
{
    public class CartItem
    {
        public string FirebaseKey { get; set; }  // FirebaseKey để thay thế GiayID
        public string ProductName { get; set; }
        public decimal Price { get; set; }

        //public decimal Total { get; set; }
        //public decimal FinalTotal { get; set; }
        public int Quantity { get; set; }
        public int SoluongTon { get; set; }
        public string ProductId { get; set; }
        public string ProductImage { get; set; }
        //public string ProductDescription { get; set; }
        public string UserId { get; set; }
    }
}

