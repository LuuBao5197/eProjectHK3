 using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StaffController : ControllerBase
    {
        private readonly DatabaseContext _dbContext;
        
        public StaffController(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("GetAllStudent")]
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

        //API RELATED CONTEST
        [HttpGet("GetAllContest")]

        public async Task<IActionResult> GetAllContest()
        {
            var contests = await _dbContext.Contests.ToListAsync();
            return Ok(contests);
        }

        [HttpGet("GetDetailContest/{id}")]
        public async Task<IActionResult> GetDetailContest(int id)
        {
            var contest = await _dbContext.Contests.FirstOrDefaultAsync(y => y.Id == id);
            return Ok(contest);
        }

        [HttpPost("AddContest")]
        public async Task<IActionResult> AddContest(Contest contest)
        {
            await _dbContext.Contests.AddAsync(contest);
            await _dbContext.SaveChangesAsync();
            return Ok(contest);
        }

        [HttpPut("EditContest/{id}")]
        public async Task<IActionResult> EditContest(int id, Contest contest)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Data is not valid" });
                }

                if(id != contest.Id)
                {
                    return NotFound(new { message = "No result about this contest" });
                }
                
                _dbContext.Entry(contest).State = EntityState.Modified;
                await _dbContext.SaveChangesAsync();

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
    }
}
