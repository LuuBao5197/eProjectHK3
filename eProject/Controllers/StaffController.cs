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
        public async Task<IActionResult> GetDetailStudent(int id) { 
            var student = await _dbContext.Students.FirstOrDefaultAsync(x => x.Id == id);
            return Ok(student);
        }
    }
}
