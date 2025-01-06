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
            var staffs = await _dbContext.Staff.ToListAsync();
            var contests = await _dbContext.Contests.Where(c=>staffs.Select(s => s.Id)
            .ToList().Contains(c.OrganizedBy)).ToListAsync();
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
        public async Task<IActionResult> GetAllAwardReceived(int id)
        {
            var awardsReceived = await _dbContext.StudentAwards
                                             .Include(s => s.Award)
                                             .Where(s => s.StudentId == id)
                                             .ToListAsync();
            return Ok(awardsReceived);
        }

        //method get all submission highest mark
        [HttpGet("GetAllSubmission")]
        public async Task<IActionResult> GetAllSubmission()
        {
            var highestRatings = await _dbContext.RatingLevels.OrderByDescending(s => s.Mark).Take(3).ToListAsync();
            var subs = await _dbContext.Submissions
                              .Include(s => s.Student) // Lấy thông tin sinh viên
                              .Include(s => s.Contest) // Lấy thông tin cuộc thi
                              .Include(s => s.SubmissionReviews) // Lấy thông tin đánh giá bài thi
                               .Where(s => s.SubmissionReviews.Any(sr => highestRatings.Select(r => r.Mark).Contains(sr.RatingLevel.Mark))) // So sánh điểm đánh giá
                              .Take(3)
                               .ToListAsync();
            return Ok(subs);
        }


        //method get all art work in exhibition
        [HttpGet("GetAllExhibitionArtwork")]
        public async Task<IActionResult> GetAllExhibitionArtwork()
        {

            var eas = await _dbContext.ExhibitionArtworks.Include(e => e.Artwork).Include(e => e.Exhibition).ToListAsync();
            return Ok(eas);
        }

        //method get all submission_review

        [HttpGet("GetAllSubmissionReview/{id}")]
        public async Task<IActionResult> GetAllSubmissionReview(int id)
        {
            var sr = await _dbContext.SubmissionReviews.Include(s => s.Submission).Include(s => s.RatingLevel).Include(s => s.Staff).ToListAsync();
            return Ok(sr);
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
