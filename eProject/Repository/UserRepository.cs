
using Microsoft.EntityFrameworkCore;

namespace eProject.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly DatabaseContext _dbContext;
        public UserRepository(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<User> AddUserAsync(User user)
        {
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();
            return user;
        }


        public async Task<User> DeleteUserAsync(int id)
        {
            var user = await GetUserById(id);
            _dbContext.Users.Remove(user);
            await _dbContext.SaveChangesAsync();
            return user;
        }

        public async Task<User> GetUserById(int Id)
        {
            var user = await _dbContext.Users
                    .FirstOrDefaultAsync(x => x.Id == Id);
            return user;
        }
        public async Task<IEnumerable<User>> GetUsersAsync()
        {
            var users = await _dbContext.Users.ToListAsync();
            return users;
        }

        public async Task<User> UpdateUser(User userExisting)
        {
            _dbContext.Users.Entry(userExisting).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync();
            return userExisting;
        }
    }
}
