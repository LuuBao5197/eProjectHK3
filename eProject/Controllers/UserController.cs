
using eProject.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly DatabaseContext _dbContext;

        public UserController(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _dbContext.Users.ToListAsync();
            return Ok(users);
        }


        [HttpGet("{id}")]
        public IActionResult GetUser(int id)
        {
            var exhibition = _dbContext.Exhibitions.Find(id);
            if (exhibition == null) return NotFound();
            return Ok(exhibition);
        }


        [HttpPost]
        public IActionResult CreateUser(User user)
        {
            _dbContext.Users.Add(user);
            _dbContext.SaveChanges();
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }

        [HttpPut("{id}")]
        public IActionResult UpdateUser(int id, User user)
        {
            _dbContext.Users.Update(user);
            _dbContext.SaveChanges();
            return Ok(user);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteUser(int id)
        {
            var user = _dbContext.Users.Find(id);
            if (user == null) return NotFound();
            _dbContext.Users.Remove(user);
            _dbContext.SaveChanges();
            return NoContent();
        }
    }
}
