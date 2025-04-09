using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace UserService.Domain.Interfaces;

public interface IUserRepository
{
        Task<IEnumerable<IdentityUser>> GetAllAsync();
        Task<IdentityUser?> GetByIdAsync(string id);
        Task AddAsync(IdentityUser user, string password);
        Task UpdateAsync(IdentityUser user);
        Task DeleteAsync(string id);
}