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

        //method get one students
        [HttpGet("GetDetailStudent/{id}")]

        public async Task<IActionResult> GetDetailStudent(int id)
        {
            var student = await _dbContext.Students.FirstOrDefaultAsync(x => x.Id == id);
            return Ok(student);
        }

        //method get all contest
        [HttpGet("GetAllContest")]
        public async Task<IActionResult> GetContest()
        {
            var contests = await _dbContext.Contests.ToListAsync();
            return Ok(contests);
        }

        //method post new submission 
        [HttpPost("CreateNewSubmission/{id}")]
        public Task<IActionResult> CreateNewSubmission(Student student, IFormFile file)
        {
            throw new NotImplementedException();
        }

        //method get all award received
        [HttpGet("GetAllAwardReceived/{id}")]
        public Task<IActionResult> GetAllAwardReceived()
        {
            throw new NotImplementedException();
        }

        //method get all submission highest mark
        [HttpGet("GetAllSubmission")]
        public Task<IActionResult> GetAllSubmission()
        {
            throw new NotImplementedException();
        }


        //method get all art work in exhibition
        [HttpGet("GetAllExhibitionArtwork")]
        public Task<IActionResult> GetAllExhibitionArtwork()
        {

            throw new NotImplementedException();
        }

        //method get all submission_review
        
        [HttpGet("GetAllSubmissionReview/{id}")]
        public Task<IActionResult> GetAllSubmissionReview(int id)
        {
            throw new NotImplementedException();
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
