using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContestController : ControllerBase
    {
        private readonly DatabaseContext _dbContext;

        public ContestController(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllContests()
        {
            var contest = await _dbContext.Contests.ToListAsync();
            return Ok(contest);
        }


        [HttpGet("{id}")]
        public IActionResult GetContest(int id)
        {
            var contest = _dbContext.Contests.Find(id);
            if (contest == null) return NotFound();
            return Ok(contest);
        }


        [HttpPost]
        public IActionResult CreateContest(Contest contest)
        {
            _dbContext.Contests.Add(contest);
            _dbContext.SaveChanges();
            return CreatedAtAction(nameof(GetContest), new { id = contest.Id }, contest);
        }

        [HttpPut("{id}")]
        public IActionResult UpdateContest(int id, Contest contest)
        {
            _dbContext.Contests.Update(contest);
            _dbContext.SaveChanges();
            return Ok(contest);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteContest(int id)
        {
            var contest = _dbContext.Contests.Find(id);
            if (contest == null) return NotFound();
            _dbContext.Contests.Remove(contest);
            _dbContext.SaveChanges();
            return NoContent();
        }
    }
}
