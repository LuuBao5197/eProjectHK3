
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
            var user = await GetUserByIdAsync(id);
            _dbContext.Users.Remove(user);
            await _dbContext.SaveChangesAsync();
            return user;
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            var user = await _dbContext.Users.FindAsync(id);
                return user;
        }

        public async Task<IEnumerable<User>> GetUsersAsync()
        {
            var users = await _dbContext.Users.ToListAsync();
            return users;
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            var userExisting = await GetUserByIdAsync(user.Id);
             _dbContext.Entry(userExisting).CurrentValues.SetValues(user);
            await _dbContext.SaveChangesAsync();
            return user;

        }
    }
}
