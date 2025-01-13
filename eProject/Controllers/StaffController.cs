 using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MUSICAPI.Helpers;
using System;

namespace eProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StaffController : ControllerBase
    {
        private readonly DatabaseContext _dbContext;
        private readonly string contestFolder = "contestThumbnail";
        private readonly string exhibitionFolder = "exhibitionThumbnail";
        public StaffController(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
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
        public async Task<IActionResult> GetAllContest(int page = 1, int pageSize = 10, string? search = null)
        {
            var query = _dbContext.Contests.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.Name.Contains(search));
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
    }
}
