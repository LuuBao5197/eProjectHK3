using eProject.Helpers;
using eProject.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity.Data;

namespace eProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private IConfiguration _configuration;

        public AuthController(IUserRepository userRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _configuration = configuration;
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login(UserAuth userAuth)
        {
            var users = await _userRepository.GetUsersAsync();
            if (users.Count() == 0)
            {
                return NotFound("user not found");
            }
            User user = users.FirstOrDefault(u => u.Email == userAuth.Email
                && u.Password == userAuth.Password);
            if (user == null)
            {
                return BadRequest("invalid credential");
            }
            var tokenString = GenerateToken(user);
            user.RefreshToken = Guid.NewGuid().ToString();
            user.RefreshTokenExpired = DateTime.UtcNow.AddDays(7);
            await _userRepository.UpdateUser(user);
            return Ok(new { token = tokenString, refreshToken = user.RefreshToken });
        }
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefrestToken(TokenRequest tokenRequest)
        {
            var users = await _userRepository.GetUsersAsync();
            if (users.Count() == 0)
            {
                return NotFound("user not found");
            }
            var user = users.FirstOrDefault
                (u => u.RefreshToken == tokenRequest.RefreshToken);
            if (user == null || user.RefreshTokenExpired <= DateTime.UtcNow)
            {
                return Unauthorized();
            }
            var tokenString = GenerateToken(user);
            user.RefreshToken = Guid.NewGuid().ToString();
            user.RefreshTokenExpired = DateTime.UtcNow.AddDays(7);
            await _userRepository.UpdateUser(user);
            return Ok(new { token = tokenString, refreshToken = user.RefreshToken });
        }

        private string GenerateToken(User user)
        {
            //lay cau hinh Jwt tu appsetting.json
            var jwtSetting = _configuration.GetSection("Jwt");
            //tao mot doi tuong JwtSecurityTokenhandler de tao va ky token
            var tokenHandler = new JwtSecurityTokenHandler();
            //chuyen doi khoa bi mat tu cau hinh thanh mang byte 
            var key = Encoding.ASCII.GetBytes(jwtSetting["Key"]!);
            //tao doi tuong Security
            //noi chua thong tin cua Token
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                //Subject chua cac claim cua nguoi dung(ten,email,role,..)
                Subject = new ClaimsIdentity(new Claim[]
               {
                   new Claim(ClaimTypes.Name,user.Name),
                    new Claim(ClaimTypes.Role,user.Role),
                     new Claim(ClaimTypes.Email,user.Email),
                        new Claim("Id", user.Id.ToString()),
               }),
                Expires = DateTime.UtcNow.AddMinutes(10),
                //thong tin de ky token, su dung khoa
                //bi mat va thuat toan HmacSha256
                SigningCredentials = new SigningCredentials
               (new SymmetricSecurityKey(key),
               SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var users = await _userRepository.GetUsersAsync();
            var user = users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                return NotFound("User not found");
            }

            // Tạo OTP ngẫu nhiên
            var otp = new Random().Next(100000, 999999).ToString(); // OTP 6 chữ số
            user.OTP = otp;
            user.OTPExpired = DateTime.UtcNow.AddMinutes(5); // OTP hết hạn sau 5 phút
            await _userRepository.UpdateUser(user);

            // Gửi OTP qua email
            SendEmail(user.Email, "Reset Password OTP", $"Your OTP is: {otp}");

            return Ok("OTP has been sent to your email.");
        }



        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOTP(VerifyOTPRequest request)
        {
            var users = await _userRepository.GetUsersAsync();
            var user = users.FirstOrDefault(u => u.Email == request.Email);

            if (user == null)
            {
                return NotFound("User not found");
            }

            if (user.OTP != request.OTP || user.OTPExpired < DateTime.UtcNow)
            {
                return BadRequest("Invalid or expired OTP");
            }

            return Ok("OTP verified");
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(NewPasswordRequest request)
        {
            var users = await _userRepository.GetUsersAsync();
            var user = users.FirstOrDefault(u => u.Email == request.Email);

            if (user == null)
            {
                return NotFound("User not found");
            }

            // Cập nhật mật khẩu mới
            user.Password = request.NewPassword;

            // Xóa OTP và ngày hết hạn OTP sau khi cập nhật mật khẩu
            user.OTP = null; // Đặt OTP thành null để tránh tái sử dụng
            user.OTPExpired = null; // Đặt ngày hết hạn OTP thành null

            // Lưu lại người dùng với mật khẩu đã được thay đổi
            await _userRepository.UpdateUser(user);

            return Ok("Password has been reset successfully.");
        }



        private void SendEmail(string toEmail, string subject, string body)
        {
            // Đọc cấu hình email từ appsettings.json
            var emailSettings = _configuration.GetSection("EmailSettings");

            var smtpClient = new SmtpClient
            {
                Host = emailSettings["Host"], // Địa chỉ SMTP server (ví dụ: smtp.gmail.com)
                Port = int.Parse(emailSettings["Port"]), // Port SMTP (ví dụ: 587)
                Credentials = new NetworkCredential(emailSettings["Username"], emailSettings["Password"]), // Tài khoản email
                EnableSsl = bool.Parse(emailSettings["EnableSsl"]) // Bật SSL
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(emailSettings["Username"]), // Địa chỉ email gửi đi
                Subject = subject,
                Body = body,
                IsBodyHtml = true // Nội dung email là HTML
            };

            mailMessage.To.Add(toEmail); // Địa chỉ email nhận

            // Gửi email
            smtpClient.Send(mailMessage);
        }
    }
    public class TokenRequest
    {
        public string RefreshToken { get; set; }
    }
}
