using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SneakerSportStore.Models
{
    public class CompleteRegisterModel
    {
        [Required, EmailAddress]
        public string Email { get; set; }

        [Required] public string HoTen { get; set; }
        [Required] public string TenDangNhap { get; set; }
        [Required] public string DiaChi { get; set; }
        [Required] public string SoDienThoai { get; set; }

        [Required, MinLength(8)]
        public string Password { get; set; }

        [Required, Compare(nameof(Password))]
        public string ConfirmPassword { get; set; }

        [Required]
        public string PendingId { get; set; }
    }
}
