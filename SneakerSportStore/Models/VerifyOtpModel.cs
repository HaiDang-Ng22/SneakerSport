//using System;
//using System.Collections.Generic;
//using System.ComponentModel.DataAnnotations;
//using System.Linq;
//using System.Web;

//namespace SneakerSportStore.Models
//{
//    public class VerifyOtpModel
//    {
//        [Required]
//        [RegularExpression(@"^\d{6}$", ErrorMessage = "Mã OTP gồm 6 chữ số")]
//        public string Code { get; set; }

//        [Required]
//        [EmailAddress]
//        public string Email { get; set; }
//    }
//}