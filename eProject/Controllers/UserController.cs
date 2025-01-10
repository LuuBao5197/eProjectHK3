
using eProject.Helpers;
using eProject.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MUSICAPI.Helpers;

namespace eProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly DatabaseContext _dbContext;

        private readonly IUserRepository _userRepository;
        private readonly string subFolder = "UserAvatar";

        public UserController(DatabaseContext dbContext, IUserRepository userRepository)
        {
            _dbContext = dbContext;
            _userRepository = userRepository;
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


        //[HttpPost]
        //public IActionResult CreateUser(User user)
        //{
        //    _dbContext.Users.Add(user);
        //    _dbContext.SaveChanges();
        //    return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        //}

        [HttpPost]
        public IActionResult CreateUser(CreateUserDto userDto)
        {
            if (userDto == null)
            {
                return BadRequest("Invalid data.");
            }

            var user = new User
            {
                Username = userDto.Username,
                Password = userDto.Password,
                Role = userDto.Role,
                Name = userDto.Name,
                Dob = userDto.Dob,
                Email = userDto.Email,
                Phone = userDto.Phone,
                JoinDate = DateTime.UtcNow, // Gán ngày tham gia mặc định
                Expired = DateTime.UtcNow.AddYears(1) // Gán ngày hết hạn mặc định
            };


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

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromForm] UpdateUserDto UpdateuserDto, IFormFile? file)
        {
            object response = null;
            try
            {
                var userExisting = await _userRepository.GetUserById(id);
                if (userExisting == null)
                {
                    response = new APIRespone(StatusCodes.Status404NotFound, "User not found", null);
                    return NotFound(response);
                }

                if (ModelState.IsValid)
                {
                    // Update image if a new file is uploaded
                    if (file != null)
                    {
                        var imagePath = await UploadFile.SaveImage(subFolder, file);
                        if (!string.IsNullOrEmpty(userExisting.Imagepath))
                        {
                            UploadFile.DeleteImage(userExisting.Imagepath);
                        }
                        userExisting.Imagepath = imagePath;
                    }

                    // Update properties only if new values are provided
                    userExisting.Name = !string.IsNullOrEmpty(UpdateuserDto.Name) ? UpdateuserDto.Name : userExisting.Name;
                    userExisting.Email = !string.IsNullOrEmpty(UpdateuserDto.Email) ? UpdateuserDto.Email : userExisting.Email;
                    userExisting.Password = !string.IsNullOrEmpty(UpdateuserDto.Password) ? UpdateuserDto.Password : userExisting.Password;
                    userExisting.Imagepath = !string.IsNullOrEmpty(UpdateuserDto.Imagepath) ? UpdateuserDto.Imagepath : userExisting.Imagepath;

                    var userUpdated = await _userRepository.UpdateUser(userExisting);
                    response = new APIRespone(StatusCodes.Status200OK, "User updated successfully", userUpdated);
                    return Ok(response);
                }

                response = new APIRespone(StatusCodes.Status400BadRequest, "Bad request", null);
                return BadRequest(response);
            }
            catch (Exception ex)
            {
                response = new APIRespone(StatusCodes.Status500InternalServerError, ex.Message, null);
                return new ObjectResult(response);
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
