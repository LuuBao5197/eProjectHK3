

namespace eProject.Repository
{
    public interface IUserRepository
    {
        Task<IEnumerable<User>> GetUsersAsync();
        Task<User> GetUserById(int id);
        Task<User> AddUserAsync(User user);
        Task<User> UpdateUser(User user);
        Task<User> DeleteUserAsync(int id);

    }
}
