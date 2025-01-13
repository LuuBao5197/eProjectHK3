﻿using Microsoft.AspNetCore.Http;
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
                .Include(s => s.Submissions) // Lấy tất cả Submissions của học sinh
                .ThenInclude(sub => sub.SubmissionReviews) // Lấy tất cả SubmissionReviews của từng Submission
                .ThenInclude(review => review.Staff) // Lấy thông tin Staff cho mỗi SubmissionReview
                .Include(s => s.User) // Lấy thông tin User của học sinh
                .FirstOrDefaultAsync(s => s.Id == id); // Tìm học sinh theo ID

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

        [HttpGet("GetExhibitionDetail/{id}")]
        //Method Get exhibition by id
        public async Task<IActionResult> GetExhibitionDetail(int id)
        {
            var exhibition = await _dbContext.Exhibitions.FindAsync(id);
            if (exhibition == null)
            {
                return NotFound("Can't Find This Award");
            }
            return Ok(exhibition);
        }
    }
}
