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
            var contests = await _dbContext.Contests
                                           .Include(c => c.Organizer)  // Ensure the organizer is included
                                           .ThenInclude(o => o.User)  // Ensure the user's name is included
                                           .Where(c => c.Status == "Published") // Filter contests with Published status
                                           .Select(c => new
                                           {
                                               c.Id,
                                               c.Name,
                                               c.Description,
                                               c.StartDate,
                                               c.EndDate,
                                               c.SubmissionDeadline,
                                               c.CreatedAt,
                                               c.UpdatedAt,
                                               OrganizerName = c.Organizer != null && c.Organizer.User != null ? c.Organizer.User.Name : null, // Get organizer's name
                                               c.Status,
                                               c.Thumbnail,
                                               c.Phase,
                                               TotalSubmissions = _dbContext.Submissions.Count(s => s.ContestId == c.Id), // Total submissions for the contest
                                               PendingSubmissions = _dbContext.Submissions.Count(s => s.ContestId == c.Id && string.IsNullOrEmpty(s.Status)), // Pending submissions
                                               ReviewedSubmissions = _dbContext.Submissions.Count(s => s.ContestId == c.Id && s.Status == "Reviewed") // Reviewed submissions
                                           })
                                           .ToListAsync();

            return Ok(contests);
        }



        [HttpGet("{id}")]
        public async Task<IActionResult> GetContest(int id)
        {
            var contest = await _dbContext.Contests
                .Include(c => c.Organizer)  // Include organizer details
                .ThenInclude(o => o.User)   // Include user's name for organizer
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contest == null) return NotFound();

            return Ok(new
            {
                contest.Id,
                contest.Name,
                contest.Description,
                contest.StartDate,
                contest.EndDate,
                contest.SubmissionDeadline,
                contest.CreatedAt,
                contest.UpdatedAt,
                OrganizerName = contest.Organizer != null && contest.Organizer.User != null ? contest.Organizer.User.Name : null,
                contest.Status
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateContest([FromBody] Contest contest)
        {
            if (contest == null)
                return BadRequest("Contest data cannot be null");

            _dbContext.Contests.Add(contest);
            await _dbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetContest), new { id = contest.Id }, contest);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateContest(int id, [FromBody] Contest contest)
        {
            if (contest == null || contest.Id != id)
                return BadRequest("Invalid contest data");

            var existingContest = await _dbContext.Contests.FindAsync(id);
            if (existingContest == null) return NotFound();

            existingContest.Name = contest.Name;
            existingContest.Description = contest.Description;
            existingContest.StartDate = contest.StartDate;
            existingContest.EndDate = contest.EndDate;
            existingContest.SubmissionDeadline = contest.SubmissionDeadline;
            existingContest.Status = contest.Status;
            existingContest.UpdatedAt = DateTime.Now;

            await _dbContext.SaveChangesAsync();

            return Ok(existingContest);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContest(int id)
        {
            var contest = await _dbContext.Contests.FindAsync(id);
            if (contest == null) return NotFound();

            _dbContext.Contests.Remove(contest);
            await _dbContext.SaveChangesAsync();

            return NoContent();
        }

    }
}
