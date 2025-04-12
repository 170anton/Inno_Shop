using System.ComponentModel.DataAnnotations;

namespace UserService.Application.DTOs
{
    public class ForgotPasswordModel
    {
        [Required]
        public string Email { get; set; } = string.Empty;
    }
}