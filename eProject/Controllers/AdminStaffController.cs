using eProject.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AddminStaffController : ControllerBase
    {
        private readonly DatabaseContext _dbContext;

        public AddminStaffController(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpPost("staff")]
        public async Task<IActionResult> AddStaff(CreateStaffRequest request)
        {
            if (request == null)
            {
                return BadRequest("Invalid data.");
            }

            // Tạo đối tượng User
            var user = new User
            {
                Username = request.Username,
                Password = request.Password,
                Role = "staff", // Gán role là staff
                Name = request.Name,
                Email = request.Email,
                Phone = request.Phone,
                Dob = request.Dob.ToString("yyyy-MM-dd"),
                Status = true,
                JoinDate = request.JoinDate,
                Expired = DateTime.MaxValue,
                Token = Guid.NewGuid().ToString()
            };

            // Lưu User vào cơ sở dữ liệu
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Tạo đối tượng Staff và gán UserId cho Staff
            var staff = new Staff
            {
                UserId = user.Id, // Gán UserId đã tạo cho Staff
                JoinDate = request.JoinDate,

                // Gán các mối quan hệ cho Staff
                Classes = request.ClassIds?.Select(classId => new Class { Id = classId }).ToList(),
                StaffSubjects = request.StaffSubjectIds?.Select(subjectId => new StaffSubject { SubjectId = subjectId }).ToList(),
                StaffQualifications = request.StaffQualificationIds?.Select(qualificationId => new StaffQualification { QualificationId = qualificationId }).ToList(),
                OrganizedContests = request.ContestIds?.Select(contestId => new Contest { Id = contestId }).ToList(),
                OrganizedExhibitions = request.ExhibitionIds?.Select(exhibitionId => new Exhibition { Id = exhibitionId }).ToList(),
                SubmissionReviews = request.SubmissionReviewIds?.Select(reviewId => new SubmissionReview { Id = reviewId }).ToList()
            };

            // Lưu Staff vào cơ sở dữ liệu
            _dbContext.Staff.Add(staff);
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Staff created successfully", staff = staff });
        }
        [HttpGet("getall")]
        public async Task<IActionResult> GetAllStaff()
        {
            // Lấy tất cả nhân viên có role là "staff" từ cơ sở dữ liệu
            var staffList = await _dbContext.Users
                                            .Where(u => u.Role == "staff")  // Lọc theo Role "staff"
                                            .Include(u => u.Staff)  // Lấy thông tin Staff kèm theo User
                                            .ToListAsync();

            if (staffList == null || staffList.Count == 0)
            {
                return NotFound("Không có nhân viên nào trong hệ thống.");
            }

            return Ok(staffList);
        }
        // Cập nhật thông tin Staff
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStaff(int id, CreateStaffRequest request)
        {
            if (request == null)
            {
                return BadRequest("Invalid data.");
            }

            // Tìm Staff theo id
            var staff = await _dbContext.Staff
                                        .Include(s => s.Classes)
                                        .Include(s => s.StaffSubjects)
                                        .Include(s => s.StaffQualifications)
                                        .Include(s => s.OrganizedContests)
                                        .Include(s => s.OrganizedExhibitions)
                                        .Include(s => s.SubmissionReviews)
                                        .FirstOrDefaultAsync(s => s.Id == id);

            if (staff == null)
            {
                return NotFound("Staff not found.");
            }

            // Cập nhật thông tin User
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == staff.UserId);
            if (user != null)
            {
                user.Username = request.Username;
                user.Password = request.Password;
                user.Name = request.Name;
                user.Email = request.Email;
                user.Phone = request.Phone;
                user.Dob = request.Dob.ToString("yyyy-MM-dd");
                user.JoinDate = request.JoinDate;
                await _dbContext.SaveChangesAsync();
            }

            // Cập nhật các mối quan hệ
            if (request.ClassIds != null)
            {
                staff.Classes = await _dbContext.Classes
                    .Where(c => request.ClassIds.Contains(c.Id))
                    .ToListAsync();
            }

            if (request.StaffSubjectIds != null)
            {
                staff.StaffSubjects = await _dbContext.StaffSubjects
                    .Where(s => request.StaffSubjectIds.Contains(s.Id))
                    .ToListAsync();
            }

            if (request.StaffQualificationIds != null)
            {
                staff.StaffQualifications = await _dbContext.StaffQualifications
                    .Where(s => request.StaffQualificationIds.Contains(s.Id))
                    .ToListAsync();
            }

            if (request.ContestIds != null)
            {
                staff.OrganizedContests = await _dbContext.Contests
                    .Where(c => request.ContestIds.Contains(c.Id))
                    .ToListAsync();
            }

            if (request.ExhibitionIds != null)
            {
                staff.OrganizedExhibitions = await _dbContext.Exhibitions
                    .Where(e => request.ExhibitionIds.Contains(e.Id))
                    .ToListAsync();
            }

            if (request.SubmissionReviewIds != null)
            {
                staff.SubmissionReviews = await _dbContext.SubmissionReviews
                    .Where(s => request.SubmissionReviewIds.Contains(s.Id))
                    .ToListAsync();
            }

            // Lưu thay đổi vào cơ sở dữ liệu
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Staff updated successfully", staff = staff });
        }
        // Xóa Staff
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStaff(int id)
        {
            // Tìm Staff theo id
            var staff = await _dbContext.Staff
                                        .Include(s => s.Classes)
                                        .Include(s => s.StaffSubjects)
                                        .Include(s => s.StaffQualifications)
                                        .Include(s => s.OrganizedContests)
                                        .Include(s => s.OrganizedExhibitions)
                                        .Include(s => s.SubmissionReviews)
                                        .FirstOrDefaultAsync(s => s.Id == id);

            if (staff == null)
            {
                return NotFound("Staff not found.");
            }

            // Xóa Staff từ cơ sở dữ liệu
            _dbContext.Staff.Remove(staff);

            // Xóa User tương ứng
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == staff.UserId);
            if (user != null)
            {
                _dbContext.Users.Remove(user);
            }

            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Staff deleted successfully." });
        }
        


    }

}
