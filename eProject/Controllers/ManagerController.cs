using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ManagerController : ControllerBase
    {
        //ConnectDB
        private readonly DatabaseContext _dbContext;
        public ManagerController(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }
        //Method Get All Class
        [HttpGet("GetAllClass")]
        public async Task<IActionResult> GetAllClass()
        {
            var classes = await _dbContext.Classes.ToListAsync();
            if (classes == null || classes.Count == 0)
            {
                return NotFound("There's no class");
            }
            return Ok(classes);
        }

        [HttpGet("GetStudentByClass/{classId}")]
        public async Task<IActionResult> GetStudentByClass(int classId)
        {
            var classWithDetails = await _dbContext.Classes
                .Include(c => c.StudentClasses)
                    .ThenInclude(sc => sc.Student)
                .Include(c => c.Staff)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(c => c.Id == classId);

            if (classWithDetails == null)
            {
                return NotFound("Class not found");
            }

            var students = classWithDetails.StudentClasses
                .Where(sc => sc.ClassId == classId)
                .Select(sc => sc.Student)
                .ToList();

            if (!students.Any())
            {
                return NotFound("No students found in this class");
            }

            var teacherName = classWithDetails.Staff?.User?.Name ?? "No teacher assigned";

            var result = new
            {
                ClassName = classWithDetails.Name,
                SchoolYear = classWithDetails.Year,
                TeacherName = teacherName,
                Students = students
            };

            return Ok(result);
        }



        //Method Get All Student
        [HttpGet("GetAllStudent")]
        public async Task<IActionResult> GetAllStudent()
        {
            var students = await _dbContext.Students.ToListAsync();
            if (students == null || students.Count == 0)
            {
                return NotFound("Can't Find Any Student");
            }
            return Ok(students);
        }
        //Method Get Student Detail By Id
        [HttpGet("GetStudentDetail/{id}")]
        public async Task<IActionResult> GetStudentDetail(int id)
        {
            var student = await _dbContext.Students
                .Include(s => s.Submissions) 
                .ThenInclude(sub => sub.SubmissionReviews) 
                .ThenInclude(review => review.Staff) 
                .Include(s => s.User) 
                .FirstOrDefaultAsync(s => s.Id == id); 

            if (student == null)
            {
                return NotFound("Can't Find This Student");
            }

            return Ok(student);
        }



        //Method Get All Contest 
        [HttpGet("GetAllContest")]
        public async Task<IActionResult> GetAllContest()
        {
            var contests = await _dbContext.Contests.ToListAsync();
            if (contests == null)
            {
                return NotFound("Can't Find Any Contest");
            }
            return Ok(contests);
        }

        //Method Get Detail Contest By Id
        [HttpGet("GetContestDetail/{id}")]
        public async Task<IActionResult> GetContestDetail(int id)
        {
            var contest = await _dbContext.Contests.FindAsync(id);
            if (contest == null)
            {
                return NotFound("Can't Find This Contest");
            }
            return Ok(contest);
        }

        //Method Get all the prizes that have been awarded
        [HttpGet("GetAllAward")]
        public async Task<IActionResult> GetAllAward()
        {
            var awards = await _dbContext.Awards.ToListAsync();
            if (awards == null)
            {
                return NotFound("Can't Find Any Award");
            }
            return Ok(awards);
        }

        //[HttpGet("GetAwardDetail/{id}")]
        //Method Get Detail of the prizes that have been awarded By Id
        [HttpGet("GetAwardDetail/{id}")]

        public async Task<IActionResult> GetAwardDetail(int id)
        {
            var award = await _dbContext.StudentAwards.FindAsync(id);
            if (award == null)
            {
                return NotFound("Can't Find This Award");
            }
            return Ok(award);
        }

        //[HttpGet("GetAllSubmissions")]
        //Method Get All Submissions
        [HttpGet("GetAllSubmissions")]

        public async Task<IActionResult> GetAllSubmissions()
        {
            var submissions = await _dbContext.Submissions.ToListAsync();
            if (submissions == null)
            {
                return NotFound("Can't Find Any Submissions");
            }
            return Ok(submissions);
        }

        //Method Get Submissions Review Detail
        [HttpGet("GetSubmissionsReviewDetail/{submissionId}/{staffId}")]
        public async Task<IActionResult> GetSubmissionsReviewDetail(int submissionId, int staffId)
        {
            // Sử dụng FirstOrDefaultAsync để truy vấn với khóa composite
            var reviewDetail = await _dbContext.SubmissionReviews
                .Include(sr => sr.Submission)      // Bao gồm thông tin bài nộp
                .Include(sr => sr.Staff)           // Bao gồm thông tin nhân viên đánh giá
                .Include(sr => sr.RatingLevel)     // Bao gồm thông tin mức độ đánh giá
                .FirstOrDefaultAsync(sr => sr.SubmissionId == submissionId && sr.StaffId == staffId);  // Truy vấn với cả hai khóa

            // Kiểm tra nếu không tìm thấy đánh giá
            if (reviewDetail == null)
            {
                return NotFound("Không tìm thấy đánh giá bài nộp này");
            }

            // Trả về chi tiết đánh giá bài nộp với các trường theo yêu cầu
            var submissionReviewDetail = new
            {
                // SubmissionId từ SubmissionReview
                SubmissionId = reviewDetail.SubmissionId,

                // StaffId từ SubmissionReview
                StaffId = reviewDetail.StaffId,

                // RatingId từ SubmissionReview (giả sử RatingId là một trường trong SubmissionReview)
                RatingId = reviewDetail.RatingLevel?.Id ?? 0,  // Kiểm tra RatingLevel và trả về RatingId nếu có

                // ReviewDate từ SubmissionReview
                ReviewDate = reviewDetail.ReviewDate.ToString("dd/MM/yyyy"),

                // ReviewText từ SubmissionReview
                ReviewText = reviewDetail.ReviewText,

                // Thông tin bài nộp (Submission)
                SubmissionDescription = reviewDetail.Submission?.Description ?? "Không có mô tả",
                SubmissionStatus = reviewDetail.Submission?.Status ?? "Chưa có trạng thái",

                // Thông tin nhân viên (Staff)
                StaffName = reviewDetail.Staff?.UserId ?? 0
            };

            // Trả về thông tin chi tiết
            return Ok(submissionReviewDetail);
        }


        //Method Get all artwork that have been send to exhibition
        [HttpGet("GetAllExhibition")]

        public async Task<IActionResult> GetAllExhibition()
        {
            var exhibitions = await _dbContext.Exhibitions.ToListAsync();
            if (exhibitions == null)
            {
                return NotFound("There's nothing");
            }
            return Ok(exhibitions);
        }
        //Method Get exhibition by id
        [HttpGet("GetExhibitionDetail/{id}")]
        public async Task<IActionResult> GetExhibitionDetail(int id)
        {
            var exhibition = await _dbContext.Exhibitions.FindAsync(id);
            if (exhibition == null)
            {
                return NotFound("Can't Find This Award");
            }
            return Ok(exhibition);
        }
        //Method Get Teacher Detail
        [HttpGet("GetTeacherDetail/{id}")]
        public async Task<IActionResult> GetTeacherDetail(int id)
        {
            var teacher = await _dbContext.Staff.FindAsync(id);
            if(teacher == null)
            {
                return NotFound("Can't Found This Teacher");
            }
            return Ok(teacher);
        }
    }
}
