using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


[Route("api/[controller]")]
[ApiController]
public class ClassController : ControllerBase
{
    private readonly DatabaseContext _context;

    public ClassController(DatabaseContext context)
    {
        _context = context;
    }

    // Lấy danh sách tất cả các lớp
    [HttpGet]
    public async Task<IActionResult> GetAllClasses()
    {
        var classes = await _context.Classes.ToListAsync();
        return Ok(classes);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetClassById(int id)
    {
        // Lấy thông tin lớp và danh sách học sinh kèm thông tin từ User
        var classEntity = await _context.Classes
            .Include(c => c.StudentClasses)
                .ThenInclude(sc => sc.Student)
                    .ThenInclude(s => s.User) // Bao gồm thông tin từ bảng User
            .FirstOrDefaultAsync(c => c.Id == id);

        if (classEntity == null)
        {
            return NotFound(); // Trả về 404 nếu không tìm thấy lớp
        }

        // Tạo dữ liệu trả về
        var classDetail = new
        {
            Id = classEntity.Id,
            Name = classEntity.Name,
            TotalStudent = classEntity.StudentClasses?.Count ?? 0,
            Students = classEntity.StudentClasses?.Select(sc => new
            {
                sc.Student.Id,
                sc.Student.User.Name, // Lấy tên từ bảng User
                sc.Student.User.Email, // Các thông tin khác từ bảng User

            })
        };

        return Ok(classDetail);
    }



    // Tạo một lớp mới
    [HttpPost]
    public async Task<IActionResult> CreateClass([FromBody] Class newClass)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        await _context.Classes.AddAsync(newClass);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetClassById), new { id = newClass.Id }, newClass);
    }

    // Cập nhật thông tin một lớp
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateClass(int id, [FromBody] Class updatedClass)
    {
        if (id != updatedClass.Id || !ModelState.IsValid)
        {
            return BadRequest();
        }

        var classEntity = await _context.Classes.FindAsync(id);
        if (classEntity == null)
        {
            return NotFound();
        }

        classEntity.Name = updatedClass.Name;
        classEntity.TotalStudent = updatedClass.TotalStudent;

        _context.Classes.Update(classEntity);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // Xóa một lớp
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteClass(int id)
    {
        var classEntity = await _context.Classes.FindAsync(id);
        if (classEntity == null)
        {
            return NotFound();
        }

        _context.Classes.Remove(classEntity);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
