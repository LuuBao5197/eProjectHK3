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
        public async Task<IActionResult> AddStudent(CreateStudentRequest request)
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

                    // Tạo đối tượng User
                    var user = new User
                    {
                        Username = request.Username,
                        Password = request.Password,
                        Role = "student", // Gán role là student
                        Name = request.Name,
                        Email = request.Email,
                        Phone = request.Phone,
                        Dob = request.Dob.ToString("yyyy-MM-dd"),
                        Status = true,
                        JoinDate = request.JoinDate,
                        Expired = DateTime.MaxValue
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

                    return Ok(new { message = "Student created successfully, email sent", student = student });
                }
                catch (Exception)
                {
                    // Rollback transaction nếu có lỗi xảy ra
                    await transaction.RollbackAsync();
                    return StatusCode(500, "An error occurred while creating the student.");
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
                                for (int col = 1; col <= 11; col++)
                                {
                                    fields.Add(worksheet.Cells[row, col]?.Text);
                                }

                                if (fields.Count < 10 || fields.Take(10).Any(string.IsNullOrEmpty))
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
                                    Expired = DateTime.MaxValue
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

                                if (fields.Count > 10 && !string.IsNullOrEmpty(fields[10]))
                                {
                                    var studentClass = new StudentClass
                                    {
                                        ClassId = int.Parse(fields[10].Trim()),
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
        //Cập nhật Student
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStudent(int id, CreateStudentRequest request)
        {
            if (request == null)
            {
                return BadRequest("Invalid data.");
            }

            // Tìm Student theo id
            var student = await _dbContext.Students
                                          .Include(s => s.StudentClasses)
                                          .Include(s => s.Submissions)
                                          .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
            {
                return NotFound("Student not found.");
            }

            // Cập nhật thông tin User
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == student.UserId);
            if (user != null)
            {
                user.Username = request.Username;
                user.Password = request.Password;
                user.Name = request.Name;
                user.Email = request.Email;
                user.Phone = request.Phone;
                user.Dob = request.Dob.ToString("yyyy-MM-dd");
                user.JoinDate = request.JoinDate;
                await _dbContext.SaveChangesAsync();
            }

            // Cập nhật các mối quan hệ
            if (request.ClassIds != null)
            {
                student.StudentClasses = (ICollection<StudentClass>?)await _dbContext.Classes.Where(c => request.ClassIds.Contains(c.Id))
                    .ToListAsync();
            }

            if (request.SubmissionIds != null)
            {
                student.Submissions = await _dbContext.Submissions
                    .Where(s => request.SubmissionIds.Contains(s.Id))
                    .ToListAsync();
            }

            /*  if (request.AwardIds != null)
              {
                  student.StudentAwards = await _dbContext.StudentAwards
                      .Where(s => request.AwardIds.Contains(s.Id))
                      .ToListAsync();
              }*/

            // Lưu thay đổi vào cơ sở dữ liệu
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Student updated successfully", student = student });
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
