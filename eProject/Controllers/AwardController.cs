using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        // Lấy danh sách giải thưởng của sinh viên, bỏ lọc theo ngày
        var result = _dbContext.StudentAwards
            .Include(sa => sa.Student) // Lấy thông tin sinh viên
            .ThenInclude(s => s.User)  // Lấy thông tin người dùng của sinh viên
            .Include(sa => sa.Award)  // Lấy thông tin giải thưởng
            .ThenInclude(a => a.Contest) // Lấy thông tin cuộc thi
            .OrderByDescending(sa => sa.Award.Contest.EndDate) // Sắp xếp theo ngày kết thúc cuộc thi
            .Select(sa => new
            {
                StudentName = sa.Student.User.Name,  // Tên sinh viên
                AwardName = sa.Award.Name,           // Tên giải thưởng
                ContestName = sa.Award.Contest.Name, // Tên cuộc thi
            })
            .ToList();

        return Ok(result);
    }
}
