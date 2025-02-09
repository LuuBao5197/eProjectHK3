using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace eProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExhibitionController : ControllerBase
    {
        private readonly DatabaseContext _dbContext;

        public ExhibitionController(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllExhibitions()
        {
            var exhibitions = await _dbContext.Exhibitions
                .Include(e => e.Organizer) // Tải kèm thông tin người tổ chức
                .ThenInclude(s => s.User) // Tải kèm thông tin User của người tổ chức
                .Where(e => e.Status == "Published") // Chỉ lấy các exhibition có trạng thái Published
                .Select(e => new
                {
                    e.Id,
                    e.Name,
                    e.StartDate,
                    e.EndDate,
                    e.Location,
                    e.thumbnail,
                    e.Phase,
                    OrganizerName = e.Organizer != null && e.Organizer.User != null ? e.Organizer.User.Name : null
                })
                .ToListAsync();

            return Ok(exhibitions);
        }


        [HttpGet("{id}")]
        public IActionResult GetExhibition(int id)
        {
            var exhibition = _dbContext.Exhibitions.Find(id);
            if (exhibition == null) return NotFound();
            return Ok(exhibition);
        }


        [HttpPost]
        public IActionResult CreateExhibition(Exhibition exhibition)
        {
            _dbContext.Exhibitions.Add(exhibition);
            _dbContext.SaveChanges();
            return CreatedAtAction(nameof(GetExhibition), new { id = exhibition.Id }, exhibition);
        }

        [HttpPut("{id}")]
        public IActionResult UpdateExhibition(int id, Exhibition exhibition)
        {
            _dbContext.Exhibitions.Update(exhibition);
            _dbContext.SaveChanges();
            return Ok(exhibition);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteExhibition(int id)
        {
            var exhibition = _dbContext.Exhibitions.Find(id);
            if (exhibition == null) return NotFound();
            _dbContext.Exhibitions.Remove(exhibition);
            _dbContext.SaveChanges();
            return NoContent();
        }
    }
}
