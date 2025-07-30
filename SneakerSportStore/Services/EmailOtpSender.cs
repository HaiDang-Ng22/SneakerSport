using System;
using System.Configuration;
using System.Net.Mail;
using System.Threading.Tasks;

namespace SneakerSportStore.Services
{
    public class EmailOtpSender : IOtpSender
    {
        private readonly string _from;

        public EmailOtpSender()
        {
            _from = ConfigurationManager.AppSettings["SmtpFrom"]
                    ?? "no-reply@sneakersportstore.com";
        }

        public async Task SendOtpAsync(string toEmail, string otp)
        {
            using (var msg = new MailMessage())
            {
                msg.From = new MailAddress(_from, "SneakerSportStore");
                msg.To.Add(toEmail);
                msg.Subject = "Mã OTP xác thực";
                msg.IsBodyHtml = true;
                msg.Body = BuildBody(otp);

                using (var client = new SmtpClient()) // đọc config trong web.config
                {
                    await client.SendMailAsync(msg);
                }
            }
        }

        private string BuildBody(string otp) => $@"
            <div style='font-family:Arial,sans-serif;font-size:14px;color:#222'>
              <p>Mã OTP của bạn (hiệu lực 5 phút):</p>
              <div style='font-size:24px;font-weight:700;letter-spacing:4px'>{otp}</div>
              <p>Nếu không phải bạn yêu cầu, hãy bỏ qua email này.</p>
            </div>";
    }
}
