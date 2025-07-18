using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SneakerSportStore.Models
{
    public class BankTransferDetails
    {
        public string BankName { get; set; }
        public string AccountNumber { get; set; }
        public string AccountHolder { get; set; }
        public double Amount { get; set; }
        public string OrderId { get; set; }
    }

}