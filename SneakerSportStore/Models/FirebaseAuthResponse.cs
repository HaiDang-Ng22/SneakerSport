using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
namespace SneakerSportStore.Models
{
    public class FirebaseAuthResponse
    {
        public string IdToken { get; set; }
        public string Email { get; set; }
        public string RefreshToken { get; set; }
        public string ExpiresIn { get; set; }
        public string LocalId { get; set; }
    }
}
