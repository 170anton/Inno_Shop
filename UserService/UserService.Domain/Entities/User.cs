using Microsoft.AspNetCore.Identity;

namespace UserService.Domain.Entities;

public class User : IdentityUser
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string CustomRole { get; set; } = string.Empty;
}