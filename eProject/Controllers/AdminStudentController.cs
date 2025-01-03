using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AddminStudentController : ControllerBase
    {
        private readonly DatabaseContext _dbContext;

        public AddminStudentController(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }
        // Tạo Student

        [HttpPost("student")]
        public async Task<IActionResult> AddStudent(CreateStudentRequest request)
        {
            if (request == null)
            {
                return BadRequest("Invalid data.");
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
                Expired = DateTime.MaxValue,
           /*     Token = Guid.NewGuid().ToString()*/
                //Token = Guid.NewGuid().ToString()
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

            return Ok(new { message = "Student created successfully", student = student });
        }
        // Lấy tất cả Student
        [HttpGet("getall")]
        public async Task<IActionResult> GetAllStudents()
        {
            var students = await _dbContext.Students.ToListAsync();

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
        // Lấy thông tin Student theo ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetStudentById(int id)
        {
            var student = await _dbContext.Students
                                          .Include(s => s.User)
                                          .Include(s => s.StudentClasses)
                                          .Include(s => s.Submissions)
                                          .Include(s => s.StudentAwards)
                                          .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
            {
                return NotFound("Student not found.");
            }

            return Ok(student);
        }
        //Tìm kiếm Student theo tên
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
