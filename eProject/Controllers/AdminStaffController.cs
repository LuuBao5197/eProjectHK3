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
                return BadRequest("Dữ liệu không hợp lệ.");
            }

            // Xử lý tải lên ảnh
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

            // Tạo đối tượng User
            var user = new User
            {
                Username = request.Username,
                Password = request.Password,
                Role = request.Role,
                Name = request.Name,
                Email = request.Email,
                Phone = request.Phone,
                Address = request.Address, // Thêm địa chỉ
                Imagepath = imagePath, // Thêm đường dẫn ảnh
                JoinDate = request.JoinDate,
                Dob = request.Dob.ToString("yyyy-MM-dd"),
               
                Expired = DateTime.UtcNow.AddYears(1)
            };

            // Lưu User vào cơ sở dữ liệu
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Tạo đối tượng Staff
            var staff = new Staff
            {
                UserId = user.Id,
                JoinDate = request.JoinDate,
                IsReviewer = false
            };

            // Lưu Staff vào cơ sở dữ liệu
            _dbContext.Staff.Add(staff);
            await _dbContext.SaveChangesAsync();

            // Xử lý các mối quan hệ Subject (nếu có)
            if (request.StaffSubjectIds != null)
            {
                foreach (var subjectId in request.StaffSubjectIds)
                {
                    var subjectExists = await _dbContext.Subjects.AnyAsync(s => s.Id == subjectId);
                    if (!subjectExists)
                    {
                        return BadRequest($"Môn học với ID {subjectId} không tồn tại.");
                    }
                    var staffSubject = new StaffSubject
                    {
                        StaffId = staff.Id,
                        SubjectId = subjectId
                    };
                    _dbContext.StaffSubjects.Add(staffSubject);
                }
            }

            // Xử lý các mối quan hệ Qualification (nếu có)
            if (request.StaffQualificationIds != null)
            {
                foreach (var qualificationId in request.StaffQualificationIds)
                {
                    var qualificationExists = await _dbContext.Qualifications.AnyAsync(q => q.Id == qualificationId);
                    if (!qualificationExists)
                    {
                        return BadRequest($"Bằng cấp với ID {qualificationId} không tồn tại.");
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
                Subject = "Chào mừng đến hệ thống của chúng tôi",
                HtmlContent = $"Kính chào {user.Name},<br/><br/>" +
                              "Tài khoản của bạn đã được tạo thành công. Dưới đây là thông tin đăng nhập:<br/>" +
                              $"<b>Tên đăng nhập:</b> {user.Username}<br/>" +
                              $"<b>Mật khẩu:</b> {user.Password}<br/><br/>" +
                              "Trân trọng,<br/>Đội ngũ"
            };

            // Gửi email
            try
            {
                await _emailService.SendMailAsync(emailRequest);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi gửi email: {ex.Message}");
            }

            return Ok(new { message = "Tạo nhân viên thành công, email đã được gửi", staff = staff });
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

        [HttpDelete("{id}")]
        public async Task<IActionResult> LockStaff(int id)
        {
            // Find the Staff member by id
            var staff = await _dbContext.Staff
                                        .Include(s => s.User)
                                        .FirstOrDefaultAsync(s => s.Id == id);

            if (staff == null)
            {
                return NotFound("Staff not found.");
            }

            // Lock the User account by setting the Status to false (inactive)
            var user = staff.User;
            user.Status = false; // Mark the account as inactive
            _dbContext.Users.Update(user);

            // Save changes to the database
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Staff account has been locked successfully." });
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UnlockStaff(int id)
        {
            // Tìm kiếm nhân viên theo id
            var staff = await _dbContext.Staff
                                        .Include(s => s.User)
                                        .FirstOrDefaultAsync(s => s.Id == id);

            if (staff == null)
            {
                return NotFound("Staff not found.");
            }

            // Mở khóa tài khoản của người dùng bằng cách đặt Status thành true (hoạt động)
            var user = staff.User;
            user.Status = true; // Đặt tài khoản thành hoạt động
            _dbContext.Users.Update(user);

            // Lưu thay đổi vào cơ sở dữ liệu
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Staff account has been unlocked successfully." });
        }


        [HttpPut("staff/{id}")]
        public async Task<IActionResult> UpdateStaff(int id, [FromForm] CreateStaffRequest request, IFormFile profileImage)
        {
            if (request == null)
            {
                return BadRequest("Dữ liệu không hợp lệ.");
            }

            // Tìm nhân viên theo id
            var staff = await _dbContext.Staff
                                        .Include(s => s.User)
                                        .FirstOrDefaultAsync(s => s.Id == id);

            if (staff == null)
            {
                return NotFound("Nhân viên không tồn tại.");
            }

            // Cập nhật thông tin User
            var user = staff.User;
            user.Username = request.Username ?? user.Username;
            user.Password = request.Password ?? user.Password;
            user.Role = request.Role ?? user.Role;
            user.Name = request.Name ?? user.Name;
            user.Email = request.Email ?? user.Email;
            user.Phone = request.Phone ?? user.Phone;
            user.Address = request.Address ?? user.Address;
            user.Dob = request.Dob.ToString("yyyy-MM-dd");



            

            // Xử lý tải lên ảnh mới (nếu có)
            string imagePath = user.Imagepath;
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

            user.Imagepath = imagePath;

            // Lưu thay đổi thông tin User
            _dbContext.Users.Update(user);

            // Cập nhật thông tin Staff
            

            // Cập nhật các mối quan hệ Subject (nếu có)
            if (request.StaffSubjectIds != null)
            {
                // Xóa các mối quan hệ Subject cũ
                _dbContext.StaffSubjects.RemoveRange(staff.StaffSubjects);

                // Thêm các mối quan hệ Subject mới
                foreach (var subjectId in request.StaffSubjectIds)
                {
                    var subjectExists = await _dbContext.Subjects.AnyAsync(s => s.Id == subjectId);
                    if (!subjectExists)
                    {
                        return BadRequest($"Môn học với ID {subjectId} không tồn tại.");
                    }

                    var staffSubject = new StaffSubject
                    {
                        StaffId = staff.Id,
                        SubjectId = subjectId
                    };
                    _dbContext.StaffSubjects.Add(staffSubject);
                }
            }

            // Cập nhật các mối quan hệ Qualification (nếu có)
            if (request.StaffQualificationIds != null)
            {
                // Xóa các mối quan hệ Qualification cũ
                _dbContext.StaffQualifications.RemoveRange(staff.StaffQualifications);

                // Thêm các mối quan hệ Qualification mới
                foreach (var qualificationId in request.StaffQualificationIds)
                {
                    var qualificationExists = await _dbContext.Qualifications.AnyAsync(q => q.Id == qualificationId);
                    if (!qualificationExists)
                    {
                        return BadRequest($"Bằng cấp với ID {qualificationId} không tồn tại.");
                    }

                    var staffQualification = new StaffQualification
                    {
                        StaffId = staff.Id,
                        QualificationId = qualificationId
                    };
                    _dbContext.StaffQualifications.Add(staffQualification);
                }
            }

            // Lưu các thay đổi vào cơ sở dữ liệu
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Cập nhật nhân viên thành công", staff = staff });
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
