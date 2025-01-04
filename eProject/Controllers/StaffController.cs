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

        //API RELATED CONTEST
        /*        [HttpGet("GetAllContest")]
                public async Task<IActionResult> GetAllContest(int page = 1, int pageSize = 6, string? search = null)
                {
                    var query = _dbContext.Contests.AsQueryable();

                    if (!string.IsNullOrEmpty(search))
                    {
                        query = query.Where(c => c.Name.Contains(search));
                    }

                    if (page <= 0 || pageSize <= 0)
                    {
                        return BadRequest("Page and pageSize must be greater than 0.");
                    }

                    // Tính toán số lượng mục bỏ qua (skip)
                    var skip = (page - 1) * pageSize;

                    // Lấy tổng số lượng contests
                    var totalItems = await _dbContext.Contests.CountAsync();

                    // Lấy dữ liệu phân trang
                    var contests = await query
                        .OrderBy(c => c.Id) // Sắp xếp theo ID (hoặc tiêu chí phù hợp)
                        .Skip(skip)
                        .Take(pageSize)
                        .ToListAsync();

                    // Trả về dữ liệu cùng thông tin phân trang
                    var response = new
                    {
                        contests = contests,
                        totalItems = totalItems,
                        totalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                        currentPage = page
                    };

                    return Ok(response);
                }*/

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
