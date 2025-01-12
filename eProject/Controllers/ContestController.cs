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
                                           .Select(c => new
                                           {
                                               c.Id,
                                               c.Name,
                                               c.Description,
                                               c.StartDate,
                                               c.EndDate,
                                               c.SubmissionDeadline,
                                               c.ParticipationCriteria,
                                               c.CreatedAt,
                                               c.UpdatedAt,
                                               OrganizerName = c.Organizer != null && c.Organizer.User != null ? c.Organizer.User.Name : null, // Lấy tên người tổ chức
                                               c.IsActive
                                           })
                                           .ToListAsync();

            return Ok(contests);
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

        [HttpGet("GetContestSubmissionStats")]
        public async Task<IActionResult> GetContestSubmissionStats()
        {
            var currentDate = DateTime.Now;

            // Lấy tất cả các cuộc thi đang diễn ra
            var ongoingContests = await _dbContext.Contests
                .Where(c => c.StartDate <= currentDate && c.EndDate >= currentDate && c.IsActive)
                .ToListAsync();

            var result = new List<ContestSubmissionStat>();

            foreach (var contest in ongoingContests)
            {
                var submissions = await _dbContext.Submissions
                    .Where(s => s.ContestId == contest.Id)
                    .ToListAsync();

                var totalSubmissions = submissions.Count();
                var pendingSubmissions = submissions.Count(s => string.IsNullOrEmpty(s.Status));
                var reviewedSubmissions = submissions.Count(s => s.Status == "Reviewed");

                result.Add(new ContestSubmissionStat
                {
                    ContestName = contest.Name,
                    TotalSubmissions = totalSubmissions,
                    PendingSubmissions = pendingSubmissions,
                    ReviewedSubmissions = reviewedSubmissions
                });
            }

            return Ok(result);
        }
    }
    public class ContestSubmissionStat
    {
        public string ContestName { get; set; }
        public int TotalSubmissions { get; set; }
        public int PendingSubmissions { get; set; }
        public int ReviewedSubmissions { get; set; }
    }
}
