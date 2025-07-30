using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SneakerSportStore.Models
{
    public class OtpVerificationModel
    {
        [Required]
        public string PendingId { get; set; }

        [Required, StringLength(6, MinimumLength = 6)]
        public string Otp { get; set; }

        public string Email { get; set; }
    }
}