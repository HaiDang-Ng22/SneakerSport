using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SneakerSportStore.Models
{
    public class DeleteAccountModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu xác nhận")]
        public string Password { get; set; }
    }
}