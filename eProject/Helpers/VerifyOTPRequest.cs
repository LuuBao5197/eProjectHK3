using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Configuration;

namespace eProject.Helpers
{
    public class VerifyOTPRequest
    {
        public string Email { get; set; }
        public string OTP { get; set; }
        public string NewPassword { get; set; }

        


    }
}
