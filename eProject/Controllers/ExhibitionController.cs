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
            var exhibitions = await (from exhibition in _dbContext.Exhibitions
                                     join user in _dbContext.Users on exhibition.OrganizedBy equals user.Id
                                     select new
                                     {
                                         exhibition.Id,
                                         exhibition.Name,
                                         exhibition.StartDate,
                                         exhibition.EndDate,
                                         exhibition.Location,
                                         OrganizedBy = user.Name // Lấy tên người tổ chức từ bảng User thay vì hiện id như thông thường
                                     }).ToListAsync();

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
