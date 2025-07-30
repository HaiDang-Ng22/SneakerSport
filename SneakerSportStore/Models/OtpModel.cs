using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SneakerSportStore.Models
{
    public class OtpModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mã OTP")]
        [Display(Name = "Mã OTP")]
        public string OtpCode { get; set; }

        public string PendingUserId { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã OTP phải gồm 6 chữ số")]
        public string Code { get; set; }
    }
}