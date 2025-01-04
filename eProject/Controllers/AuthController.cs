using eProject.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

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
            await _userRepository.UpdateUserAsync(user);
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
            await _userRepository.UpdateUserAsync(user);
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
    }
    public class TokenRequest
    {
        public string RefreshToken { get; set; }
    }

}
