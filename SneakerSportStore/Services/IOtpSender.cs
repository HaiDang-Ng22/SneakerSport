using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;

namespace SneakerSportStore.Services
{
    public interface IOtpSender
    {
        Task SendOtpAsync(string toEmail, string otp);
    }
}