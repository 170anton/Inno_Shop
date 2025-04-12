using System.ComponentModel.DataAnnotations;

namespace UserService.Application.DTOs
{
    public class ResetPasswordModel
    {
        [Required]
        public string UserId { get; set; } = string.Empty;
        [Required]
        public string Token { get; set; } = string.Empty;
        [Required]
        public string NewPassword { get; set; } = string.Empty;
        [Required]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}