using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace UserService.Application.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<IdentityUser>> GetAllAsync();
        Task<IdentityUser?> GetByIdAsync(string id);
        Task<IdentityResult> RegisterUserAsync(IdentityUser user, string password);
        Task<IdentityResult> UpdateUserAsync(IdentityUser user);
        Task<IdentityResult> DeleteUserAsync(string id);

    }
}
