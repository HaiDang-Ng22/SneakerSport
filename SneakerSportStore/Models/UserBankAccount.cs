using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SneakerSportStore.Models
{
    public class UserBankAccount
    {
        public string UserId { get; set; }  // ID người dùng
        public string BankName { get; set; }  // Tên ngân hàng
        public string AccountNumber { get; set; }  // Số tài khoản ngân hàng
        public string AccountHolder { get; set; }  // Tên chủ tài khoản
    }


}