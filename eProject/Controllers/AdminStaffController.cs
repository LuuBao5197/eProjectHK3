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
        public async Task<IActionResult> CreateStaff([FromForm] CreateStaffRequest request, IFormFile profileImage)
        {
            if (request == null)
            {
                return BadRequest("Invalid data.");
            }

            // Handle image upload
            string imagePath = null;
            if (profileImage != null && profileImage.Length > 0)
            {
                var uploadFolder = Path.Combine("Uploads", "UserAvatar");
                Directory.CreateDirectory(uploadFolder);

                var fileExtension = Path.GetExtension(profileImage.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await profileImage.CopyToAsync(fileStream);
                }

                imagePath = $"/Uploads/UserAvatar/{uniqueFileName}";
            }

            // Create User object
            var user = new User
            {
                Username = request.Username,
                Password = request.Password,
                Role = request.Role,
                Name = request.Name,
                Email = request.Email,
                Phone = request.Phone,
                Address = request.Address, // Add address
                Imagepath = imagePath, // Add image path
                JoinDate = request.JoinDate,
                Dob = request.Dob.ToString("yyyy-MM-dd"),
                Expired = DateTime.UtcNow.AddYears(1)
            };

            // Save User to the database
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Create Staff object
            var staff = new Staff
            {
                UserId = user.Id,
                JoinDate = request.JoinDate,
                IsReviewer = false
            };

            // Save Staff to the database
            _dbContext.Staff.Add(staff);
            await _dbContext.SaveChangesAsync();

            // Handle Subject relationships (if any)
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

            // Handle Qualification relationships (if any)
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

            // Save all relationship-related changes
            await _dbContext.SaveChangesAsync();

            // Send welcome email to staff
            var emailRequest = new EmailRequest
            {
                ToMail = user.Email,
                Subject = "Welcome to our system",
                HtmlContent = $"Dear {user.Name},<br/><br/>" +
                              "Your account has been successfully created. Here are your login details:<br/>" +
                              $"<b>Username:</b> {user.Username}<br/>" +
                              $"<b>Password:</b> {user.Password}<br/><br/>" +
                              "Best regards,<br/>The Team"
            };

            try
            {
                await _emailService.SendMailAsync(emailRequest);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error sending email to staff: {ex.Message}");
            }

            // Send notification email to the manager
            string managerEmail = "quylakvip1@gmail.com"; // Default manager email

            var managerEmailRequest = new EmailRequest
            {
                ToMail = managerEmail,
                Subject = "New Staff Notification",
                HtmlContent = $"Dear Manager,<br/><br/>" +
                  $"A new staff member has been added to the system with the following details:<br/>" +
                  $"<b>Staff Name:</b> {user.Name}<br/>" +
                  $"<b>Email:</b> {user.Email}<br/><br/>" +
                  $"<a href='http://localhost:5173/manager/Inactive-Staff-Status'>Click here for details</a><br/><br/>" +
                  "Best regards,<br/>The Team"
            };

            try
            {
                await _emailService.SendMailAsync(managerEmailRequest);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error sending notification email to the manager: {ex.Message}" });
            }

            return Ok(new { message = "Staff created successfully, email has been sent", staff = staff });
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
            var staffList = await _dbContext.Staff.Include(s => s.User).ToListAsync();

            if (staffList == null || staffList.Count == 0)
            {
                return NotFound("No staff members found in the system.");
            }

            return Ok(staffList);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> LockStaff(int id)
        {
            var staff = await _dbContext.Staff.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == id);

            if (staff == null)
            {
                return NotFound("Staff not found.");
            }

            var user = staff.User;
            user.Status = false; // Mark account as inactive
            _dbContext.Users.Update(user);

            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Staff account has been locked successfully." });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UnlockStaff(int id)
        {
            var staff = await _dbContext.Staff.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == id);

            if (staff == null)
            {
                return NotFound("Staff not found.");
            }

            var user = staff.User;
            user.Status = true; // Activate account
            _dbContext.Users.Update(user);

            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Staff account has been unlocked successfully." });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetStaffDetails(int id)
        {
            var staff = await _dbContext.Staff
                                        .Include(s => s.User)
                                        .Include(s => s.Classes)
                                        .Include(s => s.StaffQualifications)
                                        .ThenInclude(sc => sc.Qualification)
                                        .Include(s => s.StaffSubjects)
                                        .ThenInclude(sc => sc.Subject)
                                        .FirstOrDefaultAsync(s => s.Id == id);

            if (staff == null)
            {
                return NotFound("Staff not found.");
            }

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
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStaffStatus(int id, [FromBody] UpdateStaffStatusRequest request)
        {
            if (request == null)
            {
                return BadRequest("Dữ liệu không hợp lệ.");
            }

            // Tìm kiếm nhân viên theo ID
            var staff = await _dbContext.Staff
                                        .Include(s => s.User)
                                        .FirstOrDefaultAsync(s => s.Id == id);

            if (staff == null)
            {
                return NotFound("Nhân viên không tồn tại.");
            }

            // Cập nhật trạng thái của User
            var user = staff.User;
            user.Status = request.Status;

            // Lưu thay đổi vào cơ sở dữ liệu
            _dbContext.Users.Update(user);
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Cập nhật trạng thái nhân viên thành công", staff });
        }
        [HttpPut("staff/{id}")]
        public async Task<IActionResult> UpdateStaff(int id, [FromForm] CreateStaffRequest request)
        {
            if (request == null)
            {
                return BadRequest("Invalid data.");
            }

            // Find staff by ID
            var staff = await _dbContext.Staff.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == id);
            if (staff == null)
            {
                return NotFound("Staff not found.");
            }

            var user = staff.User;
            if (user == null)
            {
                return NotFound("Associated user not found.");
            }

            // Update basic User information
            user.Username = request.Username;
            user.Password = request.Password;
            user.Role = request.Role;
            user.Name = request.Name;
            user.Email = request.Email;
            user.Phone = request.Phone;
            user.Address = request.Address;
            user.JoinDate = request.JoinDate;
            user.Dob = request.Dob.ToString("yyyy-MM-dd");

            _dbContext.Users.Update(user);
            await _dbContext.SaveChangesAsync();

            // Update Staff information
            staff.JoinDate = request.JoinDate;
            staff.IsReviewer = request.IsReviewer;

            _dbContext.Staff.Update(staff);
            await _dbContext.SaveChangesAsync();

            // Save changes to the database
            return Ok(new { message = "Staff updated successfully", staff });
        }
    }
  }
