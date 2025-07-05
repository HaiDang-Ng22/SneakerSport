using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;
using SneakerSportStore.Models;
namespace SneakerSportStore.Models
{
    public class DiscountCode
    {
        public string IdVoucher { get; set; }    
        public string CodeName { get; set; }
        public string Condition { get; set; }
        public string DiscountType { get; set; }
        public double DiscountValue { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public bool Active { get; set; }
        public bool IsPublic { get; set; }
        public double MinimumOrderValue { get; set; }
    }
}