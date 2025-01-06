
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
            var user = _dbContext.Users.Find(id);
            if (user == null) return NotFound();
            return Ok(user);
        }


        [HttpPost]
        public IActionResult CreateUser(User user)
        {
            _dbContext.Users.Add(user);
            _dbContext.SaveChanges();
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }

        [HttpPatch("{id}")]
        public IActionResult PartialUpdateUser(int id, [FromBody] UpdateUserDto userDto)
        {
            // Retrieve the existing user from the database
            var existingUser = _dbContext.Users.Find(id);
            if (existingUser == null)
            {
                return NotFound("User not found.");
            }

            // Update only the fields that are provided in the DTO
            if (!string.IsNullOrEmpty(userDto.Username))
            {
                existingUser.Username = userDto.Username;
            }
            if (!string.IsNullOrEmpty(userDto.Password))
            {
                existingUser.Password = userDto.Password; // Ensure hashing if needed
            }
            if (!string.IsNullOrEmpty(userDto.Role))
            {
                existingUser.Role = userDto.Role;
            }
            if (!string.IsNullOrEmpty(userDto.Name))
            {
                existingUser.Name = userDto.Name;
            }
            if (!string.IsNullOrEmpty(userDto.Email))
            {
                existingUser.Email = userDto.Email;
            }
            if (!string.IsNullOrEmpty(userDto.Phone))
            {
                existingUser.Phone = userDto.Phone;
            }
            if (userDto.Status.HasValue)
            {
                existingUser.Status = userDto.Status.Value;
            }

            try
            {
                // Save changes to the database
                _dbContext.SaveChanges();
                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                // Log the exception and return a meaningful error
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "An error occurred while updating the user. Please try again.");
            }
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
