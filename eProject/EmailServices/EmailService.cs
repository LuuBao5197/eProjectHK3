
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace eProject.EmailServices
{
    public class EmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            this._emailSettings = emailSettings.Value;
        }

        public async Task SendMailAsync(EmailRequest emailRequest)
        {
            var fromAddress = new MailAddress("dangminhquan9320@gmail.com");
            var toAddress = new MailAddress(emailRequest.ToMail);

            try
            {
                var smtp = new SmtpClient
                {
                    Host = _emailSettings.Host,
                    Port = _emailSettings.Port,
                    EnableSsl = _emailSettings.EnableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Credentials = new NetworkCredential(_emailSettings.UserName, _emailSettings.Password)
                };

                using var ms = new MailMessage(fromAddress, toAddress)
                {
                    Subject = emailRequest.Subject,
                    Body = emailRequest.HtmlContent,
                    IsBodyHtml = true
                };
                await smtp.SendMailAsync(ms);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SMTP Error: {ex.Message}");
            }


        }
    }
}
