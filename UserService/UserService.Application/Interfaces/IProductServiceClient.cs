namespace UserService.Application.Interfaces
{
    public interface IProductServiceClient
    {
        Task DeactivateProductsByUserIdAsync(string userId, string token);
        Task ActivateProductsByUserIdAsync(string userId, string token);
    }
}
