using eProject.EmailServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml; // Thêm thư viện
namespace eProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminStudentController : ControllerBase
    {
        private readonly DatabaseContext _dbContext;

        private readonly EmailService _emailService;

        public AdminStudentController(DatabaseContext dbContext, EmailService emailService)
        {
            _dbContext = dbContext;
            _emailService = emailService;
        }
        // Tạo Student
        [HttpPost("student")]
        public async Task<IActionResult> AddStudent([FromForm] CreateStudentRequest request, IFormFile profileImage)
        {
            if (request == null)
            {
                return BadRequest("Invalid data.");
            }

            using (var transaction = await _dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    // Kiểm tra email đã tồn tại
                    var existingUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                    if (existingUser != null)
                    {
                        return BadRequest(new { message = "Email already exists." });
                    }

                    // Xử lý tải ảnh lên
                    string imagePath = null;
                    if (profileImage != null && profileImage.Length > 0)
                    {
                        var uploadFolder = Path.Combine( "Uploads", "UserAvatar");
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
                        Role = "student",
                        Name = request.Name,
                        Email = request.Email,
                        Phone = request.Phone,
                        Address = request.Address,
                        Dob = request.Dob.ToString("yyyy-MM-dd"),
                        Status = false,
                        JoinDate = request.JoinDate,
                        Expired = DateTime.MaxValue,
                        Imagepath = imagePath // Thêm đường dẫn ảnh
                    };

                    // Lưu User vào cơ sở dữ liệu
                    _dbContext.Users.Add(user);
                    await _dbContext.SaveChangesAsync();

                    // Tạo đối tượng Student và gán UserId cho Student
                    var student = new Student
                    {
                        UserId = user.Id,
                        EnrollmentDate = request.EnrollmentDate,
                        ParentName = request.ParentName,
                        ParentPhoneNumber = request.ParentPhoneNumber,
                        StudentClasses = request.ClassIds?.Select(classId => new StudentClass { ClassId = classId }).ToList()
                    };

                    // Lưu Student vào cơ sở dữ liệu
                    _dbContext.Students.Add(student);
                    await _dbContext.SaveChangesAsync();

                    // Commit transaction
                    await transaction.CommitAsync();

                    // Gửi email thông báo
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

                    try
                    {
                        await _emailService.SendMailAsync(emailRequest);
                    }
                    catch (Exception ex)
                    {
                        return StatusCode(500, $"Error sending email: {ex.Message}");
                    }

                    return Ok(new
                    {
                        message = "Student created successfully, email sent",
                        student = student,
                        imagePath = imagePath
                    });
                }
                catch (Exception ex)
                {
                    // Rollback transaction nếu có lỗi xảy ra
                    await transaction.RollbackAsync();
                    return StatusCode(500, $"An error occurred while creating the student: {ex.Message}");
                }
            }
        }

        [HttpPost("import-students")]
        public async Task<IActionResult> ImportStudents(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File is missing or empty.");
            }

            var students = new List<Student>();
            var emailErrors = new List<string>();

            using (var transaction = await _dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    if (file.ContentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" ||
                        file.ContentType == "application/vnd.ms-excel")
                    {
                        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                        using (var stream = file.OpenReadStream())
                        using (var package = new ExcelPackage(stream))
                        {
                            var worksheet = package.Workbook.Worksheets[0];
                            int rowCount = worksheet.Dimension.Rows;

                            // Đọc và kiểm tra email trùng từ file
                            var emailsInFile = new List<string>();
                            for (int row = 2; row <= rowCount; row++)
                            {
                                var email = worksheet.Cells[row, 4]?.Text;
                                if (!string.IsNullOrEmpty(email))
                                {
                                    emailsInFile.Add(email);
                                }
                            }

                            // Kiểm tra email trùng trong file
                            var duplicatesInFile = emailsInFile.GroupBy(x => x)
                                                             .Where(g => g.Count() > 1)
                                                             .Select(g => g.Key)
                                                             .ToList();
                            if (duplicatesInFile.Any())
                            {
                                return BadRequest(new
                                {
                                    message = "Duplicate emails found in the import file",
                                    duplicateEmails = duplicatesInFile
                                });
                            }

                            // Kiểm tra email đã tồn tại trong database
                            var existingEmails = await _dbContext.Users
                                .Where(u => emailsInFile.Contains(u.Email))
                                .Select(u => u.Email)
                                .ToListAsync();

                            if (existingEmails.Any())
                            {
                                return BadRequest(new
                                {
                                    message = "Some emails already exist in the system",
                                    duplicateEmails = existingEmails
                                });
                            }

                            // Tiếp tục với quy trình import
                            for (int row = 2; row <= rowCount; row++)
                            {
                                var fields = new List<string>();
                                for (int col = 1; col <= 12; col++)  // Cập nhật số cột thành 12 để bao gồm Address
                                {
                                    fields.Add(worksheet.Cells[row, col]?.Text);
                                }

                                if (fields.Count < 11 || fields.Take(11).Any(string.IsNullOrEmpty))
                                {
                                    return BadRequest($"Invalid data format in row {row}");
                                }

                                var user = new User
                                {
                                    Username = fields[0],
                                    Password = fields[1],
                                    Role = "student",
                                    Name = fields[2],
                                    Email = fields[3],
                                    Phone = fields[4],
                                    Dob = DateTime.Parse(fields[5]).ToString("yyyy-MM-dd"),
                                    JoinDate = DateTime.Parse(fields[6]),
                                    Expired = DateTime.MaxValue,
                                    Address = fields[10]  // Gán giá trị Address từ cột 11
                                };

                                _dbContext.Users.Add(user);
                                await _dbContext.SaveChangesAsync();

                                var student = new Student
                                {
                                    UserId = user.Id,
                                    EnrollmentDate = DateTime.Parse(fields[7]),
                                    ParentName = fields[8],
                                    ParentPhoneNumber = fields[9]
                                };

                                _dbContext.Students.Add(student);
                                await _dbContext.SaveChangesAsync();

                                if (fields.Count > 11 && !string.IsNullOrEmpty(fields[11]))
                                {
                                    var studentClass = new StudentClass
                                    {
                                        ClassId = int.Parse(fields[11].Trim()),
                                        StudentId = student.Id
                                    };

                                    _dbContext.StudentClasses.Add(studentClass);
                                    student.StudentClasses = new List<StudentClass> { studentClass };
                                    await _dbContext.SaveChangesAsync();
                                }

                                students.Add(student);

                                var emailRequest = new EmailRequest
                                {
                                    ToMail = user.Email,
                                    Subject = "Welcome to Our System",
                                    HtmlContent = $"Dear {user.Name},<br/><br/>" +
                                                "Your account has been successfully created. Below are your login details:<br/>" +
                                                $"<b>Username:</b> {user.Email}<br/>" +
                                                $"<b>Password:</b> {user.Password}<br/><br/>" +
                                                "Best regards,<br/>The Team"
                                };

                                try
                                {
                                    await _emailService.SendMailAsync(emailRequest);
                                }
                                catch (Exception ex)
                                {
                                    emailErrors.Add($"Error sending email to {user.Email}: {ex.Message}");
                                }
                            }
                        }
                    }
                    else
                    {
                        return BadRequest("Only Excel files (.xls, .xlsx) are supported.");
                    }

                    await transaction.CommitAsync();

                    return Ok(new
                    {
                        message = "Students imported successfully",
                        emailErrors = emailErrors.Any() ? emailErrors : null,
                        students = students
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    var innerException = ex.InnerException != null ? ex.InnerException.Message : "No inner exception";
                    return StatusCode(500, $"Error importing students: {ex.Message}. Inner exception: {innerException}");
                }
            }
        }

        [HttpGet("classes")]
        public async Task<IActionResult> GetClasses()
        {
            try
            {
                var classes = await _dbContext.Classes.ToListAsync();

                return Ok(classes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error fetching classes: {ex.Message}");
            }
        }


        // Lấy tất cả Student
        [HttpGet("getall")]
        public async Task<IActionResult> GetAllStudents()
        {
            var students = await _dbContext.Students
                                            .Include(s=>s.User)
                                            .Include(s => s.StudentClasses)
                                            .ThenInclude(sc => sc.Class)
                                            .ToListAsync();

            if (students == null || students.Count == 0)
            {
                return NotFound("No students found.");
            }

            return Ok(students);
        }
       

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStudent(int id, [FromForm] CreateStudentRequest request, IFormFile profileImage)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            using (var transaction = await _dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    var student = await _dbContext.Students
                        .Include(s => s.StudentClasses)
                        .FirstOrDefaultAsync(s => s.Id == id);

                    if (student == null)
                    {
                        return NotFound(new { message = "Student not found" });
                    }

                    var user = await _dbContext.Users.FindAsync(student.UserId);
                    if (user == null)
                    {
                        return NotFound(new { message = "User not found" });
                    }

                    // Check if email is being changed and if it already exists
                    if (user.Email != request.Email)
                    {
                        var emailExists = await _dbContext.Users
                            .AnyAsync(u => u.Email == request.Email && u.Id != user.Id);
                        if (emailExists)
                        {
                            return BadRequest(new { message = "Email already exists." });
                        }
                    }

                    // Handle image update
                    if (profileImage != null && profileImage.Length > 0)
                    {
                        var uploadFolder = Path.Combine("Uploads", "UserAvatar");
                        Directory.CreateDirectory(uploadFolder);

                        // Delete old image if exists
                        if (!string.IsNullOrEmpty(user.Imagepath))
                        {
                            var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), uploadFolder,
                                Path.GetFileName(user.Imagepath));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        // Save new image
                        var fileExtension = Path.GetExtension(profileImage.FileName);
                        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                        var filePath = Path.Combine(uploadFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await profileImage.CopyToAsync(fileStream);
                        }

                        user.Imagepath = $"/Uploads/UserAvatar/{uniqueFileName}";
                    }

                    // Update user information
                    user.Username = request.Username;
                    user.Name = request.Name;
                    user.Email = request.Email;
                    user.Phone = request.Phone;
                    user.Address = request.Address;
                    user.Dob = request.Dob.ToString("yyyy-MM-dd");

                    // Only update password if provided
                    if (!string.IsNullOrEmpty(request.Password))
                    {
                        user.Password = request.Password;
                    }

                    _dbContext.Users.Update(user);

                    // Update student information
                    student.ParentName = request.ParentName;
                    student.ParentPhoneNumber = request.ParentPhoneNumber;
                    student.EnrollmentDate = request.EnrollmentDate;

                    // Update class assignments
                    if (request.ClassIds != null)
                    {
                        // Remove existing class assignments
                        _dbContext.StudentClasses.RemoveRange(student.StudentClasses);
                        await _dbContext.SaveChangesAsync();

                        // Add new class assignments
                        student.StudentClasses = request.ClassIds.Select(classId => new StudentClass
                        {
                            StudentId = student.Id,
                            ClassId = classId
                        }).ToList();
                    }

                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Fetch updated student data with related information
                    var updatedStudent = await _dbContext.Students
                        .Include(s => s.User)
                        .Include(s => s.StudentClasses)
                        .ThenInclude(sc => sc.Class)
                        .FirstOrDefaultAsync(s => s.Id == id);

                    return Ok(new
                    {
                        message = "Student updated successfully",
                        student = updatedStudent
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, new { message = $"An error occurred while updating the student: {ex.Message}" });
                }
            }
        }

        

        // Xóa Student
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            // Tìm Student theo id
            var student = await _dbContext.Students
                                          .Include(s => s.StudentClasses)
                                          .Include(s => s.Submissions)
                                          .Include(s => s.StudentAwards)
                                          .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
            {
                return NotFound("Student not found.");
            }

            // Xóa Student từ cơ sở dữ liệu
            _dbContext.Students.Remove(student);

            // Xóa User tương ứng
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == student.UserId);
            if (user != null)
            {
                _dbContext.Users.Remove(user);
            }

            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Student deleted successfully." });
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetStudentDetails(int id)
        {
            var student = await _dbContext.Students
                                          .Include(s => s.User)
                                          .Include(s => s.Submissions) // Bao gồm danh sách các bài nộp
                                           .Include(s => s.StudentAwards) // Bao gồm danh sách giải thưởng
                                          .Include(s => s.StudentClasses)
                                          .ThenInclude(sc => sc.Class)
                                          .FirstOrDefaultAsync(s => s.Id == id);

            return student == null
                ? NotFound("Student not found.")
                : Ok(student);
        }

        // Lấy thông tin Student theo ID
        [HttpGet("get-by-id/{id}")]
        public async Task<IActionResult> GetStudentById(int id)
        {
            var student = await _dbContext.Students
                .Include(s => s.User)
                .Include(s => s.StudentClasses)
                .ThenInclude(sc => sc.Class)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
            {
                return NotFound(new { message = "Student not found." });
            }

            return Ok(student);
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
        [HttpGet("search")]
        public async Task<IActionResult> SearchStudentsByName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return BadRequest("Name parameter is required.");
            }

            var students = await _dbContext.Students
                                           .Include(s => s.User)
                                           .Include(s => s.StudentClasses)
                                           .Include(s => s.Submissions)
                                           .Include(s => s.StudentAwards)
                                           .Where(s => s.User.Name.Contains(name))
                                           .ToListAsync();

            if (students == null || students.Count == 0)
            {
                return NotFound("No students found with the given name.");
            }

            return Ok(students);
        }
    }
}
