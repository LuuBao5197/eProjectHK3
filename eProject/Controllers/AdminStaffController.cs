using eProject.EmailServices;
using eProject.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminStaffController : ControllerBase
    {
        private readonly DatabaseContext _dbContext;
        private readonly EmailService _emailService;

        public AdminStaffController(DatabaseContext dbContext, EmailService emailService)
        {
            _dbContext = dbContext;
            _emailService = emailService;
        }

        [HttpPost("staff")]
        public async Task<IActionResult> CreateStaff([FromBody] CreateStaffRequest request)
        {
            if (request == null)
            {
                return BadRequest("Invalid data.");
            }


            // Tạo đối tượng User từ thông tin request
            var user = new User
            {
                Username = request.Username,
                Password = request.Password,
                Role = request.Role,
                Name = request.Name,
                Email = request.Email,
                Phone = request.Phone,
                JoinDate = request.JoinDate,
                Dob = request.Dob.ToString("yyyy-MM-dd"),
                Status = true,  // Assuming new users are active by default
                IsFirstLogin = true, // Assuming first login for new user
                Expired = DateTime.UtcNow.AddYears(1) // Set account expiration to 1 year
            };

            // Lưu User vào cơ sở dữ liệu
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Tạo đối tượng Staff từ thông tin request
            var staff = new Staff
            {
                UserId = user.Id,
                JoinDate = request.JoinDate,
                IsReviewer = false // Set reviewer as false by default
            };

            // Lưu Staff vào cơ sở dữ liệu
            _dbContext.Staff.Add(staff);
            await _dbContext.SaveChangesAsync();

            // Gán các mối quan hệ Subject cho Staff (nếu có)
            if (request.StaffSubjectIds != null)
            {
                foreach (var subjectId in request.StaffSubjectIds)
                {
                    var subjectExists = await _dbContext.Subjects.AnyAsync(s => s.Id == subjectId);
                    if (!subjectExists)
                    {
                        return BadRequest($"Subject with ID {subjectId} does not exist.");
                    }

                    var staffSubject = new StaffSubject
                    {
                        StaffId = staff.Id,
                        SubjectId = subjectId
                    };
                    _dbContext.StaffSubjects.Add(staffSubject);
                }
            }

            // Gán các mối quan hệ Qualification cho Staff (nếu có)
            if (request.StaffQualificationIds != null)
            {
                foreach (var qualificationId in request.StaffQualificationIds)
                {
                    var qualificationExists = await _dbContext.Qualifications.AnyAsync(q => q.Id == qualificationId);
                    if (!qualificationExists)
                    {
                        return BadRequest($"Qualification with ID {qualificationId} does not exist.");
                    }

                    var staffQualification = new StaffQualification
                    {
                        StaffId = staff.Id,
                        QualificationId = qualificationId
                    };
                    _dbContext.StaffQualifications.Add(staffQualification);
                }
            }

            // Lưu các thay đổi liên quan đến mối quan hệ
            await _dbContext.SaveChangesAsync();

            // Tạo đối tượng email để gửi
            var emailRequest = new EmailRequest
            {
                ToMail = user.Email,
                Subject = "Welcome to Our System",
                HtmlContent = $"Dear {user.Name},<br/><br/>" +
                              "Your account has been successfully created. Below are your login details:<br/>" +
                              $"<b>Username:</b> {user.Username}<br/>" +
                              $"<b>Password:</b> {user.Password}<br/><br/>" +
                              "Best regards,<br/>The Team"
            };

            // Gửi email
            try
            {
                await _emailService.SendMailAsync(emailRequest);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error sending email: {ex.Message}");
            }

            return Ok(new { message = "Staff created successfully, email sent", staff = staff });
        }

        [HttpGet("subjects")]
        public async Task<IActionResult> GetSubjects()
        {
            var subjects = await _dbContext.Subjects.ToListAsync();
            return Ok(subjects);
        }

        [HttpGet("qualifications")]
        public async Task<IActionResult> GetQualifications()
        {
            var qualifications = await _dbContext.Qualifications.ToListAsync();
            return Ok(qualifications);
        }

        [HttpGet("getall")]
        public async Task<IActionResult> GetAllStaff()
        {
            // Lấy tất cả nhân viên có role là "staff" từ cơ sở dữ liệu
            var staffList = await _dbContext.Staff.Include(s => s.User)
                                                   .ToListAsync();

            if (staffList == null || staffList.Count == 0)
            {
                return NotFound("Không có nhân viên nào trong hệ thống.");
            }

            return Ok(staffList);
        }

        // Cập nhật thông tin Staff
        // Xóa Staff
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStaff(int id)
        {
            // Tìm Staff theo id
            var staff = await _dbContext.Staff
                                        .Include(s => s.Classes)
                                        .Include(s => s.StaffSubjects)
                                        .Include(s => s.StaffQualifications)
                                        .Include(s => s.OrganizedContests)
                                        .Include(s => s.OrganizedExhibitions)
                                        .Include(s => s.SubmissionReviews)
                                        .FirstOrDefaultAsync(s => s.Id == id);

            if (staff == null)
            {
                return NotFound("Staff not found.");
            }

            // Xóa Staff từ cơ sở dữ liệu
            _dbContext.Staff.Remove(staff);

            // Xóa User tương ứng
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == staff.UserId);
            if (user != null)
            {
                _dbContext.Users.Remove(user);
            }

            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Staff deleted successfully." });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetStaffDetails(int id)
        {
            // Tìm Staff theo id và bao gồm các mối quan hệ liên quan
            var staff = await _dbContext.Staff
                                        .Include(s => s.User) // Bao gồm thông tin User
                                        .Include(s => s.Classes) // Bao gồm thông tin Classes
                                        .Include(s => s.StaffQualifications)
                                        .ThenInclude(sc => sc.Qualification )
                                        .Include(s => s.StaffSubjects)
                                        .ThenInclude(sc => sc.Subject)
                                        .FirstOrDefaultAsync(s => s.Id == id);

            if (staff == null)
            {
                return NotFound("Staff not found.");
            }

            // Trả về thông tin chi tiết Staff
            return Ok(staff);
        }
        [HttpGet("check-email")]
        public async Task<IActionResult> CheckEmailExists([FromQuery] string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email is required.");
            }

            var emailExists = await _dbContext.Users.AnyAsync(u => u.Email == email);

            return Ok(new { exists = emailExists });
        }
    }
}
