using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using UserService.Domain.Entities;

namespace UserService.Domain.Interfaces;

public interface IUserRepository
{
        Task<IEnumerable<User>> GetAllAsync();
        Task<User?> GetByIdAsync(string id);
        Task AddAsync(User user, string password);
        Task UpdateAsync(User user);
        Task DeleteAsync(string id);
}