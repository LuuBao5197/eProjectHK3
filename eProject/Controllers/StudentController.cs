using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentController : ControllerBase
    {
        private readonly DatabaseContext _dbContext;

        public StudentController(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        //method get all students
        [HttpGet("GetAllStudent")]
        public async Task<IActionResult> GetAllStudent()
        {
            var students = await _dbContext.Students.ToListAsync();
            return Ok(students);
        }

        //method get one students
        
        public async Task<IActionResult> GetDetailStudent(int id)
        {
            var student = await _dbContext.Students.FirstOrDefaultAsync(x => x.Id == id);
            return Ok(student);
        }


        //method create student
        [HttpPost("CreateOneStudent")]
        public async Task<Student> CreateOneStudent(Student student) {
            await _dbContext.Students.AddAsync(student);
            await _dbContext.SaveChangesAsync();
            return student;
        }

        //method create many students
        [HttpPost("CreateManyStudents")]
        public async Task<IActionResult> CreateManyStudents(IEnumerable<Student> students)
        {
            if (students == null || !students.Any())
            {
                return BadRequest("Danh sách sinh viên không được để trống.");
            }

            await _dbContext.Students.AddRangeAsync(students);
            await _dbContext.SaveChangesAsync();

            return Ok(students);
        }



        //method delete student
        [HttpDelete("DeleteStudent/{id}")]
        public async Task<Student> DeleteEmployee(int id)
        {
            var std = await _dbContext.Students.FirstOrDefaultAsync(s=>s.Id==id);
            _dbContext.Students.Remove(std);
            await _dbContext.SaveChangesAsync();
            return std;
        }


        //method update student
        [HttpPut("UpdateStudent/{id}")]
        public async Task<Student> UpdateEmployee(int id, Student student)
        {
            var studentExisting = await _dbContext.Students.FirstOrDefaultAsync(s => s.Id == id);
            student.Id = id;
            _dbContext.Entry(studentExisting).CurrentValues.SetValues(student);
            await _dbContext.SaveChangesAsync();
            return student;
        }
    }
}
