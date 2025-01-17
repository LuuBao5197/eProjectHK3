﻿using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MUSICAPI.Helpers;
using System;
using System.Net.Mail;
using System.Net;
using System.Net.WebSockets;

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
        public StaffController(DatabaseContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
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
            var query = _dbContext.Contests.AsQueryable();

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
                var contestCheck = await _dbContext.Contests.FirstOrDefaultAsync(c=>c.Name == contest.Name);
                if (contestCheck != null)
                {
                    return BadRequest(new { message = "NameContest must be not duplicate" });
                }
                var thumbnail = "http://localhost:5190/Uploads/DefaultSystem/DefaultContestThumbnail.jpg";
                if (file != null) {
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
        public async Task<IActionResult> EditContest(int id,[FromForm] Contest contest, IFormFile? file)
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
        public  async Task<IActionResult> DeleteContest(int id)
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
        public async Task<IActionResult> GetAllAward(int page = 1, int pageSize = 10, string? search = null)
        {
            var query = _dbContext.Awards.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.Name.Contains(search));
            }

            var totalItems = await query.CountAsync();
            var awards = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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
        .ThenInclude(st=>st!.User) // Bao gom thong tin user
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
        public async Task<IActionResult> GetAllExhibition(int page = 1, int pageSize = 10, string? search = null)
        {
            var query = _dbContext.Exhibitions.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.Name.ToLower().Contains(search.ToLower()));
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
        public async Task<IActionResult> EditExhibition(int id,[FromForm] Exhibition exhibition, IFormFile? image)
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
            var query = _dbContext.Submissions.Where(c=>c.ContestId == id).AsQueryable();

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
                .Include(s => s.SubmissionReviews) // Include để lấy review
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
                    Reviews = s.SubmissionReviews.Select(r => new
                    {
                        r.SubmissionId,
                        r.StaffId,
                        r.ReviewText,
                        r.ReviewDate
                    }).ToList(),
                    SubmissionDescription = s.Description
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
                    Reviews = s.SubmissionReviews.Select(r => new
                    {
                        r.SubmissionId,
                        r.StaffId,
                        r.ReviewText,
                        r.ReviewDate
                    }).ToList()
                })
                .ToListAsync();

            return Ok(new
            {
                submissions,
                totalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                totalItems
            });
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
                return StatusCode(StatusCodes.Status500InternalServerError,"Error network");
            }
        }

        [HttpGet("GetInfoStaff/{id}")]
        public async Task<IActionResult> GetStaffFromUserId(int id)
        {
            var staff = await _dbContext.Staff.FirstOrDefaultAsync(s => s.UserId == id);
            if (staff == null) return BadRequest("Not fount any Staff ");
            return Ok(staff);
        }

        [HttpPatch("SendContestDraftForReview/{id}")]
        public async Task<IActionResult> SendContestDraftForReview(int id)
        {
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
                SendEmail(manager.Email, "Review Draft Contest", "<p> Em da soan xong ban thao cuoc thi,  gui xep duyet giup em </p>");
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
               exhibition.status = "Pending";

                // Lưu thay đổi
                _dbContext.Exhibitions.Update(exhibition);
                await _dbContext.SaveChangesAsync();
                SendEmail(manager.Email, "Review Exhibition", "<p> Co 1 ban thao trien lam do giao vien soan dang cho su phe duyet tu ban </p>");
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
                await _dbContext.SaveChangesAsync();
                SendEmail(manager.Email, "Review Award", "<p> Co 1 ban thao giai thuong do giao vien soan dang cho su phe duyet tu ban </p>");
                return Ok(award);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
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
