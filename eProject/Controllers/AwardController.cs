using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace eProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AwardController : ControllerBase
    {
        private readonly DatabaseContext _dbContext;

        public AwardController(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("GetStudentsWithAwards")]
        public IActionResult GetStudentsWithAwards()
        {
            // Lọc các giải thưởng Top 1, Top 2, Top 3 gần nhất mà không cần trường Date
            var result = _dbContext.StudentAwards
                .Include(sa => sa.Student)
                .ThenInclude(s => s.User)
                .Include(sa => sa.Award)
                .ThenInclude(a => a.Contest)
                .Where(sa => new[] { "Top 1", "Top 2", "Top 3" }.Contains(sa.Award.Name)) // Lọc các giải thưởng Top 1, Top 2, Top 3
                .OrderByDescending(sa => sa.Award.Contest.StartDate) // Giải thưởng mới nhất dựa trên ngày bắt đầu của cuộc thi
                .Take(3) // Chỉ lấy 3 giải thưởng đầu tiên
                .Select(sa => new
                {
                    StudentName = sa.Student.User.Name,
                    AwardName = sa.Award.Name,
                    ContestName = sa.Award.Contest.Name,
                    ContestStartDate = sa.Award.Contest.StartDate // Thêm ngày bắt đầu của cuộc thi nếu cần
                })
                .ToList();

            return Ok(result);
        }
    }
}
