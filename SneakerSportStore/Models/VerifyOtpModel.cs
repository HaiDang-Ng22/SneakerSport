using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SneakerSportStore.Models
{
    public class VerifyOtpModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập OTP")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP phải gồm 6 số")]
        public string Otp { get; set; }

        public bool Resent { get; set; } // flag hiển thị thông báo
    }

}