using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SneakerSportStore.Models
{
    public class SecurityVerificationModel
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng trả lời câu hỏi bảo mật")]
        [Display(Name = "Câu trả lời bảo mật")]
        public string SecurityAnswer { get; set; }
    }
}