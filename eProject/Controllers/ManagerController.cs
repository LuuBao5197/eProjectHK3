using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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
            var classes = await _dbContext.Classes
                .Include(c => c.Staff)
                .ThenInclude(s => s.User)
                .ToListAsync();

            if (classes == null || classes.Count == 0)
            {
                return NotFound("There's no class");
            }

            var classesWithTeacher = classes.Select(c => new
            {
                c.Id,
                c.Name,
                c.Year,
                c.TotalStudent,
                TeacherName = c.Staff != null && c.Staff.User != null ? c.Staff.User.Name : "No teacher assigned"
            }).ToList();

            return Ok(classesWithTeacher);
        }

        //Method Get Student Of Class
        [HttpGet("GetStudentByClass/{classId}")]
        public async Task<IActionResult> GetStudentByClass(int classId)
        {
            var classWithDetails = await _dbContext.Classes
                .Where(c => c.Id == classId)
                .Include(c => c.StudentClasses)
                    .ThenInclude(sc => sc.Student)
                .Include(c => c.Staff)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync();

            if (classWithDetails == null)
            {
                return NotFound("Class not found");
            }

            var students = classWithDetails.StudentClasses
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
                Students = students.Select(s => new
                {
                    s.Id,
                    UserName = s.User?.Username, 
                    s.ParentName,
                    s.ParentPhoneNumber
                }).ToList()
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

        //Method Get All Value Of Awards
        [HttpGet("GetAllValue")]
        public async Task<IActionResult> GetAllValue()
        {
            var values = await _dbContext.Awards
                .Include(a => a.Contest)
                .Select(a => new
                {
                    a.Value,
                    a.Contest.StartDate
                })
                .ToListAsync();
            return Ok(values);
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
        [HttpGet("GetSubmissionsReviewDetail/{submissionId}")]
        public async Task<IActionResult> GetSubmissionsReviewDetail(int submissionId)
        {
            var reviewDetail = await _dbContext.Submissions
                .Where(s => s.Id == submissionId)
                .Include(s => s.Student)
                .Include(s => s.SubmissionReviews!)
                .ThenInclude(sr => sr.Staff)
                .ToListAsync();

            if (reviewDetail == null)
            {
                return NotFound("Can't Find This Submissions Review");
            }
            return Ok(reviewDetail);
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
            if (teacher == null)
            {
                return NotFound("Can't Found This Teacher");
            }
            return Ok(teacher);
        }

        //Method Get Meeting/Request
        [HttpGet("GetAllRequest")]
        public async Task<IActionResult> GetAllRequest()
        {
            var requests = await _dbContext.Requests.ToListAsync();
            if (requests == null)
            {
                return NotFound("There's No Request");
            }
            return Ok(requests);
        }

        [HttpPut("UpdateRequest/{id}")]
        public async Task<IActionResult> UpdateRequest(int id, Request request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return BadRequest(new { message = "Data is not valid", errors });
                }

                if (id != request.Id)
                {
                    return NotFound(new { message = "Request ID mismatch" });
                }

                var existingRequest = await _dbContext.Requests.FindAsync(id);
                if (existingRequest == null)
                {
                    return NotFound(new { message = "Request not found" });
                }

                // Kiểm tra trạng thái hợp lệ và cập nhật
                if (existingRequest.Status == "Preparing" && (request.Status == "Done" || request.Status == "Canceled"))
                {
                    existingRequest.Status = request.Status;
                }

                // Cập nhật dữ liệu còn lại nếu cần
                existingRequest.MeetingTime = request.MeetingTime;
                existingRequest.Description = request.Description;
                existingRequest.Organized = request.Organized;

                _dbContext.Entry(existingRequest).State = EntityState.Modified;
                await _dbContext.SaveChangesAsync();

                return Ok(new { message = "Request updated successfully", data = existingRequest });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPost("CreateMeeting")]
        public async Task<IActionResult> CreateMeeting([FromBody] Request request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return BadRequest(new { message = "Data is not valid", errors });
                }

                _dbContext.Requests.Add(request);
                await _dbContext.SaveChangesAsync();

                return Ok(new { message = "Meeting created successfully", data = request });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

    }
}
