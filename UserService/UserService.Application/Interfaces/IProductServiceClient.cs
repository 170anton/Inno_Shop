namespace UserService.Application.Interfaces
{
    public interface IProductServiceClient
    {
        Task DeactivateProductsByUserIdAsync(string userId);
        Task ActivateProductsByUserIdAsync(string userId);
    }
}
