using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using UserService.Infrastructure.Data;
using UserService.Infrastructure.Repositories;
using UserService.Domain.Interfaces;
using UserService.Application.Interfaces;

namespace UserService.Infrastructure;

public class ProductServiceClient : IProductServiceClient
{
    private readonly HttpClient _client;

    public ProductServiceClient(HttpClient client)
    {
        _client = client;
    }

    public async Task DeactivateProductsByUserIdAsync(string userId)
    {
        var response = await _client.PutAsync($"api/products/deactivate/{userId}", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task ActivateProductsByUserIdAsync(string userId)
    {
        var response = await _client.PutAsync($"api/products/activate/{userId}", null);
        response.EnsureSuccessStatusCode();
    }
}