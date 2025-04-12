namespace UserService.Application.DTOs
{
    public class UpdateUserModel
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }
}