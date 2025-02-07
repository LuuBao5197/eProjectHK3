using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MUSICAPI.Helpers;
using System;
using System.Net.Mail;
using System.Net;
using System.Net.WebSockets;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Linq;

namespace eProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StaffController : ControllerBase
    {
        private readonly DatabaseContext _dbContext;
        private readonly string contestFolder = "contestThumbnail";
        private readonly string exhibitionFolder = "exhibitionThumbnail";
        private IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public StaffController(DatabaseContext dbContext, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }
        private string GetCurrentUserId()
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return userId ?? "Unknown";
        }

        [HttpGet("GetAllStudent{page}")]
        public async Task<IActionResult> GetAllStudent()
        {
            var students = await _dbContext.Students.ToListAsync();
            return Ok(students);
        }
        [HttpGet("GetDetailStudent/{id}")]
        public async Task<IActionResult> GetDetailStudent(int id)
        {
            var student = await _dbContext.Students.FirstOrDefaultAsync(x => x.Id == id);
            return Ok(student);
        }


        [HttpGet("GetAllContest")]
        public async Task<IActionResult> GetAllContest(int page = 1, int pageSize = 10, string? search = null,
            int? staffId = -1, string? status = null, string? phase = null)
        {
            var query = _dbContext.Contests.Include(c => c.ContestJudge).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.Name.Contains(search));
            }
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(c => c.Status.ToLower().Equals(status.ToLower()));
            }

            if (!string.IsNullOrEmpty(phase))
            {
                query = query.Where(c => c.Phase.ToLower().Equals(phase.ToLower()));
            }
            if (staffId != -1)
            {
                query = query.Where(c => c.OrganizedBy == staffId);
            }
            var totalItems = await query.CountAsync();
            var contests = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                contests,
                totalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                totalItems
            });
        }

        [HttpGet("GetDetailContest/{id}")]
        public async Task<IActionResult> GetDetailContest(int id)
        {
            var contest = await _dbContext.Contests.FirstOrDefaultAsync(y => y.Id == id);
            return Ok(contest);
        }

        [HttpPost("AddContest")]
        public async Task<IActionResult> AddContest([FromForm] Contest contest, IFormFile? file)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Data is not valid" });
                }
                var contestCheck = await _dbContext.Contests.FirstOrDefaultAsync(c => c.Name == contest.Name);
                if (contestCheck != null)
                {
                    return BadRequest(new { message = "NameContest must be not duplicate" });
                }
                var thumbnail = "http://localhost:5190/Uploads/DefaultSystem/DefaultContestThumbnail.jpg";
                if (file != null)
                {
                    thumbnail = await UploadFile.SaveImage(contestFolder, file);
                }
                contest.Thumbnail = thumbnail;

            }
            catch (Exception ex)
            {
                return BadRequest("Add Contest Defailed");
            }
            await _dbContext.Contests.AddAsync(contest);
            await _dbContext.SaveChangesAsync();
            return Created("Success", contest);
        }

        [HttpPut("EditContest/{id}")]
        public async Task<IActionResult> EditContest(int id, [FromForm] Contest contest, IFormFile? file)
        {
            try
            {
                if (id != contest.Id)
                {
                    return NotFound(new { message = "No result about this contest" });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Data is not valid" });
                }
                var contestCheck = await _dbContext.Contests.FirstOrDefaultAsync(c => c.Name == contest.Name && c.Id != contest.Id);
                if (contestCheck != null)
                {
                    return BadRequest(new { message = "NameContest must be not duplicate" });
                }


                var oldThumbnail = contest.Thumbnail;
                if (file != null)
                {
                    contest.Thumbnail = await UploadFile.SaveImage(contestFolder, file);
                }

                _dbContext.Entry(contest).State = EntityState.Modified;
                await _dbContext.SaveChangesAsync();
                if (!string.IsNullOrEmpty(oldThumbnail) && oldThumbnail != "http://localhost:5190/Uploads/DefaultSystem/DefaultContestThumbnail.jpg")
                {
                    UploadFile.DeleteImage(oldThumbnail);
                }
                return Ok(new { message = "Contest updated successfully", data = contest });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });

            }
        }


        [HttpDelete("DeleteContest/{id}")]
        public async Task<IActionResult> DeleteContest(int id)
        {
            try
            {
                var contest = await _dbContext.Contests.FirstOrDefaultAsync(contest => contest.Id == id);
                _dbContext.Contests.Remove(contest);
                await _dbContext.SaveChangesAsync();
                return Ok(contest);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });

            }

        }

        [HttpGet("GetAllAward")]
        public async Task<IActionResult> GetAllAward(int page = 1, int pageSize = 10, string? search = null, string? status = null, string? phase = null)
        {
            var query = _dbContext.Awards.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.Name.Contains(search) || c.Contest.Name.Contains(search));
            }
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(c => c.Status.ToLower().Equals(status.ToLower()));
            }
            if (!string.IsNullOrEmpty(phase))
            {
                if (phase == "true")
                {
                    query = query.Where(c => c.IsAwarded == true);
                }
                else
                {
                    query = query.Where(c => c.IsAwarded == false);
                }
               ;
            }

            var totalItems = await query.CountAsync();
            var awards = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(a => a.Contest)
                .ToListAsync();

            return Ok(new
            {
                awards,
                totalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                totalItems
            });
        }
        [HttpGet("GetDetailAward/{id}")]
        public async Task<IActionResult> GetDetailAward(int id)
        {

            var award = _dbContext.Awards!
        .Include(a => a.Contest) // Bao gồm thông tin cuộc thi
        .Include(a => a.StudentAwards!) // Bao gồm danh sách giải thưởng sinh viên
        .ThenInclude(sa => sa.Student) // Bao gồm thông tin sinh viên
        .ThenInclude(st => st!.User) // Bao gom thong tin user
        .FirstOrDefault(a => a.Id == id);
            return Ok(award);
        }

        [HttpPost("AddAward")]
        public async Task<IActionResult> AddAward(Award award)
        {
            await _dbContext.Awards.AddAsync(award);
            await _dbContext.SaveChangesAsync();
            return Ok(award);
        }

        [HttpPut("EditAward/{id}")]
        public async Task<IActionResult> EditAward(int id, Award award)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Data is not valid" });
                }

                if (id != award.Id)
                {
                    return NotFound(new { message = "No result about this contest" });
                }

                _dbContext.Entry(award).State = EntityState.Modified;
                await _dbContext.SaveChangesAsync();

                return Ok(new { message = "Contest updated successfully", data = award });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });

            }
        }


        [HttpGet("GetAllExhibition")]
        public async Task<IActionResult> GetAllExhibition(int page = 1, int pageSize = 10, string? search = null, string? status = null, string? phase = null)
        {
            var query = _dbContext.Exhibitions.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.Name.ToLower().Contains(search.ToLower()));
            }
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(c => c.Status.ToLower().Contains(status.ToLower()));
            }
            if (!string.IsNullOrEmpty(phase))
            {
                query = query.Where(c => c.Phase.ToLower().Contains(phase.ToLower()));
            }

            var totalItems = await query.CountAsync();
            var exhibitions = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                exhibitions,
                totalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                totalItems
            });
        }

        [HttpGet("GetDetailExhibition/{id}")]
        public async Task<IActionResult> GetDetailExhibition(int id)
        {

            var exhibition = _dbContext.Exhibitions
        .FirstOrDefault(a => a.Id == id);
            return Ok(exhibition);
        }

        [HttpPost("AddExhibition")]
        public async Task<IActionResult> AddExhibition([FromForm] Exhibition exhibition, IFormFile? image)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Data is not valid" });
                }
                var exhibitionCheck = await _dbContext.Contests.FirstOrDefaultAsync(c => c.Name == exhibition.Name);
                if (exhibitionCheck != null)
                {
                    return BadRequest(new { message = "Name Exhibition must be not duplicate" });
                }
                var thumbnail = "http://localhost:5190/Uploads/DefaultSystem/ExhibitionDefaultThumbnail.jpg";
                if (image != null)
                {
                    thumbnail = await UploadFile.SaveImage(exhibitionFolder, image);
                }
                exhibition.thumbnail = thumbnail;
            }
            catch (Exception ex)
            {
                return BadRequest("Add Contest Defailed");
            }
            await _dbContext.Exhibitions.AddAsync(exhibition);
            await _dbContext.SaveChangesAsync();
            return Ok(exhibition);
        }

        [HttpPut("EditExhibition/{id}")]
        public async Task<IActionResult> EditExhibition(int id, [FromForm] Exhibition exhibition, IFormFile? image)
        {
            try
            {
                if (id != exhibition.Id)
                {
                    return NotFound(new { message = "No result about this exhibition" });
                }
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Data is not valid" });
                }
                var exhibitionCheck = await _dbContext.Contests.FirstOrDefaultAsync(c => c.Name == exhibition.Name && c.Id != exhibition.Id);
                if (exhibitionCheck != null)
                {
                    return BadRequest(new { message = "Name Exhibition must be not duplicate" });
                }
                var oldThumbnail = exhibition.thumbnail;
                if (image != null)
                {
                    exhibition.thumbnail = await UploadFile.SaveImage(exhibitionFolder, image);
                    if (!string.IsNullOrEmpty(oldThumbnail) && oldThumbnail != "http://localhost:5190/Uploads/DefaultSystem/ExhibitionDefaultThumbnail.jpg")
                    {
                        UploadFile.DeleteImage(oldThumbnail);
                    }

                }

                _dbContext.Entry(exhibition).State = EntityState.Modified;
                await _dbContext.SaveChangesAsync();

                return Ok(new { message = "Exhibition updated successfully", data = exhibition });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpDelete("DeleteExhibition/{id}")]
        public async Task<IActionResult> DeleteExhibition(int id)
        {
            try
            {
                var exhibition = await _dbContext.Exhibitions.FirstOrDefaultAsync(ex => ex.Id == id);
                _dbContext.Exhibitions.Remove(exhibition);
                await _dbContext.SaveChangesAsync();
                return Ok(exhibition);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });

            }

        }

        [HttpGet("GetSubmissionByContest/{id}")]
        public async Task<IActionResult> GetSubmissionByContest(int id, int page = 1, int pageSize = 10, string? search = null)
        {
            /*  var submissions = await _dbContext.Submissions.FirstOrDefaultAsync(s => s.ContestId == id);*/
            var query = _dbContext.Submissions.Where(c => c.ContestId == id).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.Name.ToLower().Contains(search.ToLower()));
            }

            var totalItems = await query.CountAsync();
            var submissions = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                submissions,
                totalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                totalItems
            });
        }
        [HttpGet("GetSubmissionHasReviewByContest/{id}")]
        public async Task<IActionResult> GetSubmissionHasReviewByContest(int id, int page = 1, int pageSize = 10, string? search = null, int staffId = -1)
        {
            var query = _dbContext.Submissions
                .Where(c => c.ContestId == id)
                .Include(s => s.SubmissionReviews) // Include get review
                .ThenInclude(sr => sr.RatingLevel)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.Name.ToLower().Contains(search.ToLower()));
            }

            if (staffId != -1)
            {
                // Lọc các submissions có ít nhất một review với staffId phù hợp
                query = query.Where(c => c.SubmissionReviews.Any(r => r.StaffId == staffId));
            }

            var totalItems = await query.CountAsync();
            var submissions = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new
                {
                    SubmissionId = s.Id,
                    SubmissionName = s.Name,
                    Thumbnail = s.FilePath,
                    SubmissionDescription = s.Description,
                    SubmissionDate = s.SubmissionDate,
                    Reviews = s.SubmissionReviews.ToList(),
                })
                .ToListAsync();

            return Ok(new
            {
                submissions,
                totalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                totalItems
            });
        }

        [HttpGet("GetSubmissionNotReviewByContest/{id}")]
        public async Task<IActionResult> GetSubmissionNotReviewByContest(int id, int page = 1, int pageSize = 10, string? search = null, int staffId = -1)
        {
            var query = _dbContext.Submissions
                .Where(c => c.ContestId == id)
                .Include(s => s.SubmissionReviews) // Include để lấy reviews
                .ThenInclude(sr => sr.RatingLevel)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.Name.ToLower().Contains(search.ToLower()));
            }

            if (staffId != -1)
            {
                // Lọc các submissions mà staff chưa thực hiện review
                query = query.Where(c => !c.SubmissionReviews.Any(r => r.StaffId == staffId));
            }

            var totalItems = await query.CountAsync();
            var submissions = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new
                {
                    SubmissionId = s.Id,
                    SubmissionName = s.Name,
                    SubmissionDescription = s.Description,
                    SubmissionDate = s.SubmissionDate,
                    Thumbnail = s.FilePath,
                    Reviews = s.SubmissionReviews.ToList()
                })
                .ToListAsync();

            return Ok(new
            {
                submissions,
                totalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                totalItems
            });
        }


        [HttpGet("GetReviewForSubmissionOfStaff")]
        public async Task<IActionResult> GetReviewForSubmissionOfStaff(int submissionID, int staffID)
        {
            var review = await _dbContext.SubmissionReviews.FirstOrDefaultAsync(sr => sr.SubmissionId == submissionID && sr.StaffId == staffID);
            if (review == null)
            {
                return NoContent();
            }
            return Ok(review);

        }

        [HttpGet("GetRatingLevel")]
        public async Task<IActionResult> GetRatingLevel()
        {
            var ratingLevels = await _dbContext.RatingLevels.ToListAsync();
            return Ok(ratingLevels);
        }

        [HttpPost("AddSubmissionReview")]
        public async Task<IActionResult> ReviewSubmission(SubmissionReview submissionReview)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest("Data is not valid");
                await _dbContext.SubmissionReviews.AddAsync(submissionReview);
                await _dbContext.SaveChangesAsync();
                return Created("Add review for submission successfully", submissionReview);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error network");
            }
        }


        [HttpPut("EditSubmissionReview")]
        public async Task<IActionResult> EditSubmissionReview(SubmissionReview submissionReview)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest("Data is not valid");
                _dbContext.SubmissionReviews.Update(submissionReview);
                await _dbContext.SaveChangesAsync();
                return Ok(new { submissionReview });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Error network");
            }
        }

        [HttpGet("ComputeAverageRatingForAllSubmission")]
        public async Task<IActionResult> ComputeAverageRatingForAllSubmission(int ContestID)
        {
            var submissionExisting = await _dbContext.Submissions.Where(s => s.ContestId == ContestID).Include(s => s.SubmissionReviews).ThenInclude(sr => sr.RatingLevel).ToListAsync();

            if (submissionExisting == null) return BadRequest();
            foreach (var item in submissionExisting)
            {
                if (item.SubmissionReviews != null || !item.SubmissionReviews.Any())
                {
                    double averageRating = item.SubmissionReviews.Average(sr => sr.RatingLevel.Mark);
                    item.AverageRating = averageRating;
                }
            }
            _dbContext.Submissions.UpdateRange(submissionExisting);
            await _dbContext.SaveChangesAsync();
            return Ok(submissionExisting);
        }
        [HttpGet("GetProjectedRanking")]
        public async Task<IActionResult> GetProjectedRanking(int contestId)
        {
            var submissions = await _dbContext.Submissions
              .Where(s => s.ContestId == contestId)!
              .Include(s => s.Student).ThenInclude(s => s.User)
              .Include(s => s.SubmissionReviews)!
              .ThenInclude(sr => sr.RatingLevel)
              .Include(s => s.Contest).ThenInclude(c => c.ContestJudge)
              .ToListAsync();

            if (!submissions.Any()) return BadRequest("No submissions found.");

            // Compute average
            foreach (var submission in submissions)
            {
                if (submission.SubmissionReviews != null && submission.SubmissionReviews.Any())
                {
                    submission.AverageRating = submission.SubmissionReviews.Average(sr => sr.RatingLevel.Mark);
                }
                else
                {
                    submission.AverageRating = 0;
                }
            }
            //Update Submission Table 
            _dbContext.Submissions.UpdateRange(submissions);
            await _dbContext.SaveChangesAsync();
            // Sort submissions by average descending
            submissions = submissions.OrderByDescending(s => s.AverageRating).ToList();
            return Ok(submissions);

        }

        [HttpGet("ComputeAndAssignAwards")]
        public async Task<IActionResult> ComputeAndAssignAwards(int contestId)
        {


            var contestExisting = await _dbContext.Contests.FirstOrDefaultAsync(c => c.Id == contestId);
            if (contestExisting == null) return BadRequest("No Contest Found");
            if (DateTime.Now.CompareTo(contestExisting.SubmissionDeadline) < 0)
            {
                return BadRequest("You must be compute and assign awards after SubmissionDeadline time");
            }
            var studentAwardExisting = await _dbContext.StudentAwards.Include(sa => sa.Award)
                .ThenInclude(a => a.Contest)
                .FirstOrDefaultAsync(sa => sa.Award.Contest.Id == contestId);

            if (studentAwardExisting != null) return BadRequest("Contest results already exist ");
            var submissions = await _dbContext.Submissions
                .Where(s => s.ContestId == contestId)!
                .Include(s => s.SubmissionReviews)!
                .ThenInclude(sr => sr.RatingLevel)
                 .Include(s => s.Contest).ThenInclude(c => c.ContestJudge)
                 .Include(s => s.Student).ThenInclude(s=>s.User)
                .ToListAsync();

            if (!submissions.Any()) return BadRequest("No submissions found.");
            if (submissions.Any(s => s.SubmissionReviews.Count() < s.Contest.ContestJudge.Count()))
            {
                return BadRequest("Not enought to number review required");
            }

            // Compute average
            foreach (var submission in submissions)
            {
                if (submission.SubmissionReviews != null && submission.SubmissionReviews.Any())
                {
                    submission.AverageRating = submission.SubmissionReviews.Average(sr => sr.RatingLevel.Mark);
                }
            }
            //Update Submission Table 
            _dbContext.Submissions.UpdateRange(submissions);

            // Sort submissions by average descending
            submissions = submissions.OrderByDescending(s => s.AverageRating).ToList();


            // Get List Awards by Contest
            var awards = await _dbContext.Awards
                .Where(a => a.ContestId == contestId)
                .OrderByDescending(a => a.Value)
                .ToListAsync();

            if (awards == null || !awards.Any()) return BadRequest("No awards found for this contest.");

            // Create List to save awards for Student
            var studentAwards = new List<StudentAward>();
            int submissionIndex = 0;

            // Assign award follow by limit quantity of every award
            foreach (var award in awards)
            {
                int count = 0;
                while (count < award.AwardQuantity && submissionIndex < submissions.Count)
                {
                    studentAwards.Add(new StudentAward
                    {
                        StudentId = submissions[submissionIndex].StudentId,
                        AwardId = award.Id,
                        Student = submissions[submissionIndex].Student,
                    });


                    submissionIndex++; // Xét tiếp bài dự thi tiếp theo
                    count++; // Đếm số lượng giải đã cấp
                }
            }


            // Save to StudentAwards Table
            await _dbContext.StudentAwards.AddRangeAsync(studentAwards);
            await _dbContext.SaveChangesAsync();

            return Ok(studentAwards);
        }

        [HttpGet("GetStudentAwards")]
        public async Task<IActionResult> GetStudentAwards(string? search = null, string? status = null)
        {
            var query = _dbContext.StudentAwards
                .Include(sa => sa.Award).ThenInclude(a => a.Contest)
                .Include(sa => sa.Student).ThenInclude(s => s.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(sa => sa.Student.User.Name.ToLower().Contains(search.ToLower())
                                       || sa.Award.Contest.Name.ToLower().Contains(search.ToLower()));
            }
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(sa => sa.Status.ToLower().Equals(status.ToLower()));
            }

            var studentAwards = await query
                .GroupBy(sa => sa.Award.Contest)  // Nhóm theo Contest
                .Select(group => new
                {
                    ContestName = group.Key.Name,
                    ContestId = group.Key.Id,
                    Awards = group.Select(sa => new
                    {
                        StudentName = sa.Student.User.Name,
                        StudentId = sa.Student.Id,
                        AwardName = sa.Award.Name,
                        Status = sa.Status,
                        AwardId =  sa.Award.Id,
                        
                    }).ToList()
                }).ToListAsync();

            return Ok(studentAwards);
        }


        [HttpGet("GetInfoStaff/{id}")]
        public async Task<IActionResult> GetStaffFromUserId(int id)
        {
            var staff = await _dbContext.Staff.FirstOrDefaultAsync(s => s.UserId == id);
            if (staff == null) return BadRequest("Not found any Staff ");
            return Ok(staff);
        }

        [HttpPatch("SendContestDraftForReview/{id}")]
        public async Task<IActionResult> SendContestDraftForReview(int id)
        {
            /*    var userId = User.FindFirst("Id")?.Value;*/
            /*     if (userId == null)
                 {
                     return Unauthorized("User not authenticated");
                 }*/
            var contest = await _dbContext.Contests.FindAsync(id);
            if (contest == null)
            {
                return NotFound("Contest not found.");
            }
            var manager = await _dbContext.Users.FirstOrDefaultAsync(u => u.Role == "Manager");
            if (manager == null)
            {
                return BadRequest("Manager not found");
            }

            try
            {
                contest.Status = "Pending";

                // Lưu thay đổi
                _dbContext.Contests.Update(contest);
                await _dbContext.SaveChangesAsync();
                string emailContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Approval Request</title>
</head>
<body style='font-family: Arial, sans-serif; background-color: #f4f4f4; paddinKD: 20px;'>
    <div style='max-width: 600px; margin: 0 auto; background: #ffffff; padding: 20px; 
                border-radius: 8px; box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);'>
        <h2 style='color: #333;'>Approval Request Notification</h2>
        <p>Hello <strong>Manager</strong>,</p>
        <p>A new request is awaiting your approval. Below are the details:</p>

        <table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
            <tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Contest Name:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'> {contest.Name}</td></tr>
            <tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Request Type:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>Contest Draft</td></tr>
            <tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Created Date:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>{DateTime.Now}</td></tr>
        </table>

        <p>Please click the link below to review and approve the request:</p>
        <p style='text-align: center;'>
            <a href='https://yourwebsite.com/approve-request?id=123' 
               style='background: #28a745; color: #ffffff; padding: 12px 20px; text-decoration: none; border-radius: 5px; display: inline-block;'>
               Review & Approve
            </a>
        </p>
        <p>If you did not initiate this request, please ignore this email.</p>
        <p>Best regards,<br><strong>Management System</strong></p>
    </div>
</body>
</html>";

                SendEmail(manager.Email, "Review Draft Contest", emailContent);
                return Ok(contest);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPatch("SendExhibitionForReview/{id}")]
        public async Task<IActionResult> SendExhibitionForReview(int id)
        {
            var exhibition = await _dbContext.Exhibitions.FindAsync(id);
            if (exhibition == null)
            {
                return NotFound("Exhibition not found.");
            }
            var manager = await _dbContext.Users.FirstOrDefaultAsync(u => u.Role == "Manager");
            if (manager == null)
            {
                return BadRequest("Manager not found");
            }

            try
            {
                exhibition.Status = "Pending";

                // Lưu thay đổi
                _dbContext.Exhibitions.Update(exhibition);
                await _dbContext.SaveChangesAsync();
                string emailContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Approval Request</title>
</head>
<body style='font-family: Arial, sans-serif; background-color: #f4f4f4; paddinKD: 20px;'>
    <div style='max-width: 600px; margin: 0 auto; background: #ffffff; padding: 20px; 
                border-radius: 8px; box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);'>
        <h2 style='color: #333;'>Approval Request Notification</h2>
        <p>Hello <strong>Manager</strong>,</p>
        <p>A new request is awaiting your approval. Below are the details:</p>

        <table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
            <tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Exhibition Name:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'> {exhibition.Name}</td></tr>
            <tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Request Type:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>Contest Draft</td></tr>
            <tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Created Date:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>{DateTime.Now}</td></tr>
        </table>

        <p>Please click the link below to review and approve the request:</p>
        <p style='text-align: center;'>
            <a href='https://yourwebsite.com/approve-request?id=123' 
               style='background: #28a745; color: #ffffff; padding: 12px 20px; text-decoration: none; border-radius: 5px; display: inline-block;'>
               Review & Approve
            </a>
        </p>
        <p>If you did not initiate this request, please ignore this email.</p>
        <p>Best regards,<br><strong>Management System</strong></p>
    </div>
</body>
</html>";
                SendEmail(manager.Email, "Review Exhibition", emailContent);
                return Ok(exhibition);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPatch("SendAwardForReview/{id}")]
        public async Task<IActionResult> SendAwardForReview(int id)
        {
            var userId = int.Parse(GetCurrentUserId());
            var InfoSender = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var award = await _dbContext.Awards.FindAsync(id);
            if (award == null)
            {
                return NotFound(" not found.");
            }
            var manager = await _dbContext.Users.FirstOrDefaultAsync(u => u.Role == "Manager");
            if (manager == null)
            {
                return BadRequest("Manager not found");
            }

            try
            {
                award.Status = "Pending";

                // Lưu thay đổi
                _dbContext.Awards.Update(award);
                string emailContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Approval Request</title>
</head>
<body style='font-family: Arial, sans-serif; background-color: #f4f4f4; paddinKD: 20px;'>
    <div style='max-width: 600px; margin: 0 auto; background: #ffffff; padding: 20px; 
                border-radius: 8px; box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);'>
        <h2 style='color: #333;'>Approval Request Notification</h2>
        <p>Hello <strong>Manager</strong>,</p>
        <p>A new request is awaiting your approval. Below are the details:</p>

        <table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
            <tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Award Name:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'> {award.Name}</td></tr>
            <tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Request Type:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>Contest Draft</td></tr>
            <tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Created Date:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>{DateTime.Now}</td></tr>
        </table>

        <p>Please click the link below to review and approve the request:</p>
        <p style='text-align: center;'>
            <a href='https://yourwebsite.com/approve-request?id=123' 
               style='background: #28a745; color: #ffffff; padding: 12px 20px; text-decoration: none; border-radius: 5px; display: inline-block;'>
               Review & Approve
            </a>
        </p>
        <p>If you did not initiate this request, please ignore this email.</p>
        <p>Best regards,<br><strong>Management System</strong></p>
    </div>
</body>
</html>";
                await _dbContext.SaveChangesAsync();
                SendEmail(manager.Email, "Review Award", emailContent);
                return Ok(award);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // HANDLE CHOICE JUDGE FOR CONTEST
        // INSERT MANY RECORD INTO CONTESTJUDGE TABLE

        [HttpPost("insertContestJudge")]
        public async Task<IActionResult> CreateManyContestJudge(IEnumerable<ContestJudge> contestJudges)
        {
            try
            {
                if (contestJudges == null || !contestJudges.Any())
                {
                    return BadRequest("List contest judges is empty."); // More descriptive message
                }
                foreach (var item in contestJudges)
                {
                    ContestJudge JudgeExisting = _dbContext.ContestJudges.FirstOrDefault(cj => cj.Equals(item));
                    if (JudgeExisting != null) return BadRequest("List contest judges is dupplicate with data in database");
                }

                // 4️⃣ Add new contest judges
                await _dbContext.ContestJudges.AddRangeAsync(contestJudges);
                await _dbContext.SaveChangesAsync();

                return CreatedAtAction(nameof(CreateManyContestJudge), contestJudges);
            }
            catch (DbUpdateException ex)
            // Catch DbUpdateException for database-related errors
            {
                return StatusCode(500, $"Database error: {ex.Message}"); // More specific error message
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpPut("updateManyContestJudges")]
        public async Task<IActionResult> UpdateManyContestJudges(IEnumerable<ContestJudge> contestJudges)
        {
            try
            {
                if (contestJudges == null || !contestJudges.Any())
                {
                    return BadRequest("List contest judges is empty.");
                }
                var currentContestID = contestJudges.First().ContestId;
                var oldContestJudges = await _dbContext.ContestJudges.Where(cj => cj.ContestId == currentContestID).ToListAsync();

                if (oldContestJudges == null || !oldContestJudges.Any())
                {
                    return BadRequest("No old data to find");
                }
                // Handle delete old data
                _dbContext.RemoveRange(oldContestJudges);
                // Handle create new data
                await _dbContext.AddRangeAsync(contestJudges);
                await _dbContext.SaveChangesAsync();
                return Ok("Contest judges updated successfully.");
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, $"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpGet("getAllStaff")]
        public async Task<IActionResult> GetAllStaff()
        {
            var staffs = await _dbContext.Staff.Include(s => s.User).ToListAsync();
            return Ok(staffs);
        }

        [HttpGet("getAllContestWithJudge")]
        public async Task<IActionResult> GetAllContestWithJudge(int page = 1, int pageSize = 10, string? search = null, string? status = null)
        {

            var query = _dbContext.Contests.Include(c => c.ContestJudge!).ThenInclude(cj => cj.Staff).ThenInclude(s => s.User).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.Name.ToLower().Contains(search.ToLower()));
            }
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(c => c.ContestJudge!.Any(cj => cj.status.ToLower() == status.ToLower()));
            }

            var totalItems = await query.CountAsync();
            var contests = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                contests,
                totalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                totalItems
            });



        }
        [HttpGet("getDetailContestWithJudge/{ContestID}")]
        public async Task<IActionResult> GetDetailContestWithJudge(int ContestID)
        {
            var result = await _dbContext.Contests.Where(c => c.Id == ContestID).Include(c => c.ContestJudge).ThenInclude(cj => cj.Staff).ThenInclude(s => s.User).ToListAsync();
            return Ok(result);
        }

        [HttpPatch("SendJudgeForReview/{contestID}")]
        public async Task<IActionResult> SendJudgeForReview(int contestID)
        {
            var contestJudges = await _dbContext.ContestJudges.Where(cj => cj.ContestId == contestID).Include(cj => cj.Staff).ThenInclude(s => s.User).ToListAsync();
            if (contestJudges == null || !contestJudges.Any())
            {
                return BadRequest("No contestJudges for this contest");
            }
            var manager = await _dbContext.Users.FirstOrDefaultAsync(u => u.Role == "Manager");
            if (manager == null)
            {
                return BadRequest("Manager not found");
            }

            try
            {
                foreach (var item in contestJudges)
                {
                    item.status = "Pending";
                }
                _dbContext.ContestJudges.UpdateRange(contestJudges);

                string emailContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Approval Request</title>
</head>
<body style='font-family: Arial, sans-serif; background-color: #f4f4f4; paddinKD: 20px;'>
    <div style='max-width: 600px; margin: 0 auto; background: #ffffff; padding: 20px; 
                border-radius: 8px; box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);'>
        <h2 style='color: #333;'>Approval Request Notification</h2>
        <p>Hello <strong>Manager</strong>,</p>
        <p>A new request is awaiting your approval. Below are the details:</p>

        <table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
          
            <tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Request Type:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>Judge for Contest Draft</td></tr>
            <tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Created Date:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>{DateTime.Now}</td></tr>
        </table>

        <p>Please click the link below to review and approve the request:</p>
        <p style='text-align: center;'>
            <a href='https://yourwebsite.com/approve-request?id=123' 
               style='background: #28a745; color: #ffffff; padding: 12px 20px; text-decoration: none; border-radius: 5px; display: inline-block;'>
               Review & Approve
            </a>
        </p>
        <p>If you did not initiate this request, please ignore this email.</p>
        <p>Best regards,<br><strong>Management System</strong></p>
    </div>
</body>
</html>";
                await _dbContext.SaveChangesAsync();
                SendEmail(manager.Email, "Review Judge For Contest", emailContent);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPatch("SendStudentAwardForReview/{contestID}")]
        public async Task<IActionResult> SendStudentAwardForReview(int contestID)
        {
            var studentAwards = await _dbContext.StudentAwards
                .Include(sa=>sa.Award)
                .ThenInclude(a=>a.Contest)
                .Include(sa=>sa.Student)
                .ThenInclude(st=>st.User)
                .Where(sa=>sa.Award.ContestId == contestID)
                .ToListAsync();
            if (studentAwards == null || !studentAwards.Any())
            {
                return BadRequest("No studentAwards for this contest");
            }
            var manager = await _dbContext.Users.FirstOrDefaultAsync(u => u.Role == "Manager");
            if (manager == null)
            {
                return BadRequest("Manager not found");
            }

            try
            {
                foreach (var item in studentAwards)
                {
                    item.Status = "Pending";
                }
                _dbContext.StudentAwards.UpdateRange(studentAwards);

                string emailContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Approval Request</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            background-color: #f4f4f4;
            padding: 20px;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            background: #ffffff;
            padding: 20px;
            border-radius: 8px;
            box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);
        }}
        h2 {{
            color: #333;
        }}
        table {{
            width: 100%;
            border-collapse: collapse;
            margin: 20px 0;
        }}
        th, td {{
            padding: 8px;
            border: 1px solid #ddd;
            text-align: left;
        }}
        .btn {{
            background: #28a745;
            color: #ffffff;
            padding: 12px 20px;
            text-decoration: none;
            border-radius: 5px;
            display: inline-block;
            text-align: center;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <h2>Approval Request Notification</h2>
        <p>Hello <strong>Manager</strong>,</p>
        <p>A new request is awaiting your approval. Below are the details:</p>

        <table>
            <tr>
                <td><strong>Contest Name:</strong></td>
                <td>{studentAwards.First().Award.Contest.Name}</td>
            </tr>
            <tr>
                <td><strong>Created Date:</strong></td>
                <td>{DateTime.Now}</td>
            </tr>
        </table>

        <h3>List of Awarded Students</h3>
        <table>
            <tr>
                <th>Student Name</th>
                <th>Award Name</th>
                <th>Value Award($)</th>
            </tr>";

                foreach (var award in studentAwards)
                {
                    emailContent += $@"
            <tr>
                <td>{award.Student.User.Name}</td>
                <td>{award.Award.Name}</td>
                <td>{award.Award.Value}</td>
            </tr>";
                }

                emailContent += $@"
        </table>

        <p>Please click the link below to review and approve the request:</p>
        <p style='text-align: center;'>
            <a href='https://yourwebsite.com/approve-request?id=123' class='btn'>
               Review & Approve
            </a>
        </p>
        <p>If you did not initiate this request, please ignore this email.</p>
        <p>Best regards,<br><strong>Management System</strong></p>
    </div>
</body>
</html>";

                await _dbContext.SaveChangesAsync();
                SendEmail(manager.Email, "Review StudentAward For Contest", emailContent);
                return Ok(studentAwards);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("GetSubmissionOfStudentAwards")]
        public async Task<IActionResult> GetSubmissionOfStudentAwards(string? search = null, string? status = null)
        {
            var query = _dbContext.StudentAwards
                .Include(sa => sa.Award).ThenInclude(a => a.Contest).ThenInclude(c=>c.Submissions)
                .Include(sa => sa.Student).ThenInclude(s => s.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(sa => sa.Student.User.Name.ToLower().Contains(search.ToLower())
                                       || sa.Award.Contest.Name.ToLower().Contains(search.ToLower()));
            }
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(sa => sa.Status.ToLower().Equals(status.ToLower()));
            }

            var studentAwards = await query
                .GroupBy(sa => sa.Award.Contest)  // Nhóm theo Contest
                .Select(group => new
                {
                    ContestName = group.Key.Name,
                    ContestId = group.Key.Id,
                    Awards = group.Select(sa => new
                    {
                        StudentName = sa.Student.User.Name,
                        StudentId = sa.Student.Id,
                        AwardName = sa.Award.Name,
                        Status = sa.Status,
                        AwardId = sa.Award.Id,
                        SubmissionId = sa.Award.Contest.Submissions.First(s=>s.StudentId == sa.Student.Id).Id

                    }).ToList()
                }).ToListAsync();

            return Ok(studentAwards);
        }

        [HttpPost("CreateManyArtWork")]
        public async Task<IActionResult> CreateManyArtWork(IEnumerable<Artwork> artworks)
        {
            if (artworks == null || !artworks.Any())
            {
                return BadRequest(new { message = "Artwork list cannot be empty." });
            }
            foreach (var item in artworks)
            {
                if(_dbContext.Artworks.FirstOrDefaultAsync(a=>a.SubmissionId == item.SubmissionId) != null)
                {
                    return BadRequest("Submission have been available in data");
                }
            }
            foreach (var artwork in artworks)
            {
                if (!TryValidateModel(artwork))
                {
                    return BadRequest(new { message = "Invalid data.", errors = ModelState });
                }

                artwork.Status ??= "Available";
                artwork.PaymentStatus ??= "Unpaid";
                artwork.ExhibitionDate = DateTime.Now;
            }

            await _dbContext.Artworks.AddRangeAsync(artworks);
            await _dbContext.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("GetAllArtWork")]
        public async Task<IActionResult> GetAllArtWork()
        {
            var artworks = _dbContext.Artworks.Include(a => a.Submission).ThenInclude(s => s.Student).ThenInclude(st => st.User);
            return Ok(artworks);

        }

        private void SendEmail(string toEmail, string subject, string body)
        {
            // Đọc cấu hình email từ appsettings.json
            var emailSettings = _configuration.GetSection("EmailSettings");

            var smtpClient = new SmtpClient
            {
                Host = emailSettings["Host"], // Địa chỉ SMTP server (ví dụ: smtp.gmail.com)
                Port = int.Parse(emailSettings["Port"]), // Port SMTP (ví dụ: 587)
                Credentials = new NetworkCredential(emailSettings["Username"], emailSettings["Password"]), // Tài khoản email
                EnableSsl = bool.Parse(emailSettings["EnableSsl"]) // Bật SSL
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(emailSettings["Username"]), // Địa chỉ email gửi đi
                Subject = subject,
                Body = body,
                IsBodyHtml = true // Nội dung email là HTML
            };

            mailMessage.To.Add(toEmail); // Địa chỉ email nhận

            // Gửi email
            smtpClient.Send(mailMessage);
        }
    }
}
