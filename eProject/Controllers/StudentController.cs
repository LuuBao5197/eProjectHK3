using eProject.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MUSICAPI.Helpers;

namespace eProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentController : ControllerBase
    {
        private readonly string submissionFolder = "submissionFile";

        private readonly DatabaseContext _dbContext;

        public StudentController(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        //method get all contest where status = true
        /*[HttpGet("GetAllContest")]
        public async Task<IActionResult> GetContest()
        {
            var contests = await _dbContext.Contests.Include(c=>c.Organizer.User)
                .Where(c=>c.IsActive==true ).ToListAsync();
            return Ok(contests);
        }*/

        [HttpGet("GetOneContestById/{id}")]
        public async Task<IActionResult> getOneContestById(int id)
        {
            var contest = await _dbContext.Contests.Include(c=>c.Organizer.User).FirstOrDefaultAsync(c => c.Id == id);
            return Ok(contest);
        }

        //method post new submission 
        [HttpPost("CreateNewSubmission/{contestId}")]
        public async Task<IActionResult> CreateNewSubmission([FromForm] Submission submission, IFormFile fileImage, int contestId, int studentId)
        {
            try
            {
                // Kiểm tra contestId có tồn tại trong database hay không
                var contest = await _dbContext.Contests.FindAsync(contestId);
                if (contest == null)
                {
                    return NotFound(new { Message = "Contest không tồn tại." });
                }

                if (fileImage == null || fileImage.Length == 0)
                {
                    return BadRequest(new { Message = "No file was uploaded." });
                }
                // Xử lý đường dẫn thư mục uploads và lưu tệp
                string filePath;
                try
                {
                    filePath = await UploadFile.SaveImage(submissionFolder, fileImage);
                }
                catch (Exception ex)
                {
                    // Xử lý ngoại lệ khi lưu tệp
                    return StatusCode(500, new { Message = $"Lỗi khi lưu tệp: {ex.Message}" });
                }



                // Thêm contestId vào submission
                submission.ContestId = contestId;
                submission.StudentId = studentId;
                submission.SubmissionDate = DateTime.UtcNow;
                submission.FilePath = filePath;

                // Thêm submission vào database
                await _dbContext.Submissions.AddAsync(submission);
                await _dbContext.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Tạo submission thành công!",
                    SubmissionId = submission.Id,
                    FilePath = submission.FilePath
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Lỗi xảy ra khi xử lý yêu cầu: {ex.Message}" });
            }
        }



        [HttpGet("GetOneSubmissionById/{stuId}/{subId}")]
        public async Task<IActionResult> GetOneSubmissionById(int stuId, int subId)
        {
            try
            {
                // Truy vấn danh sách submissions có StudentId trùng khớp
                var subs = await _dbContext.Submissions.Include(s => s.SubmissionReviews)
                    .Include(s => s.Contest).Include(s => s.Contest.Organizer).Include(s => s.Contest.Organizer.User)
                                           .FirstOrDefaultAsync(s => s.StudentId == stuId && s.Id == subId);
                // Nếu không tìm thấy submission nào, trả về thông báo phù hợp
                if (subs == null)
                {
                    return NotFound(new { Message = "Không tìm thấy submission nào cho sinh viên với ID được cung cấp!" });
                }

                // Trả về danh sách submissions
                return Ok(subs);
            }
            catch (Exception e)
            {
                // Xử lý lỗi và trả về thông báo lỗi
                return BadRequest(new { Message = "Đã xảy ra lỗi khi xử lý yêu cầu.", Details = e.Message });
            }
        }

        [HttpPut("UpdateSubmission")]
        public async Task<IActionResult> UpdateSubmission([FromForm] Submission submission, IFormFile fileImage, int subId)
        {
            try
            {
                // Kiểm tra xem submission có tồn tại trong database không
                var existingSubmission = await _dbContext.Submissions.FirstOrDefaultAsync(s => s.Id == subId);
                if (existingSubmission == null)
                {
                    return NotFound(new { Message = "Không tìm thấy submission với ID được cung cấp!" });
                }

                // Cập nhật thông tin từ form (nếu có)
                if (!string.IsNullOrEmpty(submission.Name))
                {
                    existingSubmission.Name = submission.Name;
                }
                if (!string.IsNullOrEmpty(submission.Description))
                {
                    existingSubmission.Description = submission.Description;
                }

                // Xử lý fileImage
                if (fileImage != null)
                {
                    var filePath = await UploadFile.SaveImage(submissionFolder, fileImage);
                    existingSubmission.FilePath = filePath;

                  
                }

                // Thực hiện lưu các thay đổi vào database
                _dbContext.Submissions.Update(existingSubmission);
                await _dbContext.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Cập nhật submission thành công!",
                    SubmissionId = existingSubmission.Id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi xảy ra khi xử lý yêu cầu: {ex.Message}");
            }
        }




        //method get all award received
        [HttpGet("GetAllAwardReceived/{id}")]
        public async Task<IActionResult> GetAllAwardReceived(int id)
        {
            var contests = await _dbContext.Contests.ToListAsync();
            var awardsReceived = await _dbContext.StudentAwards
                                             .Include(s => s.Award.Contest.Organizer.User)
                                             .Where(s => s.StudentId == id)
                                             .ToListAsync();
            return Ok(awardsReceived);
        }

        //method get all submission of one Student 
        [HttpGet("GetAllSubmission/{studentId}")]
        public async Task<IActionResult> GetAllSubmission(int studentId)
        {
            try
            {
                // Lọc các submission theo StudentId
                var submissions = await _dbContext.Submissions
                    .Where(s => s.StudentId == studentId)
                                      .Include(s => s.Student)
                                      .Include(s => s.Contest)
                                      .Include(s => s.SubmissionReviews)
                                      .ToListAsync();

                if (submissions == null || !submissions.Any())
                {
                    return NotFound(new { Message = "Không tìm thấy submission nào liên quan!" });
                }

                return Ok(submissions);
            }

            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi xảy ra khi xử lý yêu cầu: {ex.Message}");
            }
        }



        //method get all art work in exhibition
        [HttpGet("GetAllExhibitionArtwork")]
        public async Task<IActionResult> GetAllExhibitionArtwork(int studentId)
        {
            var eas = await _dbContext.ExhibitionArtworks
                .Include(e => e.Artwork)
                .Include(e => e.Exhibition.Organizer.User)
                .Include(e => e.Artwork.Submission.Student)
                .Include(e=>e.Artwork.Submission.Contest)
                .Where(e => e.Artwork.Submission.Student.Id == studentId) 
                .ToListAsync();

            return Ok(eas);
        }

        //method get all submission_review

        [HttpGet("GetAllSubmissionReview/{id}")]
        public async Task<IActionResult> GetAllSubmissionReview(int id)
        {
            var sr = await _dbContext.SubmissionReviews.Include(s => s.Submission).Include(s => s.RatingLevel).Include(s => s.Staff).ToListAsync();
            return Ok(sr);
        }


        [HttpGet("MyInformation")]
        public async Task<IActionResult> GetInforStudent(int id)
        {
            try
            {
                var student = await _dbContext.Students
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (student == null)
                {
                    return NotFound("Student not found.");
                }

                return Ok(student);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

    }
}
