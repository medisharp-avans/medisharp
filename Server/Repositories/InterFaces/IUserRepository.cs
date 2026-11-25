using Server.Models;

namespace Server.Repositories.InterFaces
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
    }
}
