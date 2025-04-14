using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using UserService.Infrastructure.Data;
using UserService.Infrastructure.Repositories;
using UserService.Domain.Interfaces;
using UserService.Application.Interfaces;
using System.Net.Http.Headers;

namespace UserService.Infrastructure.Services;

public class ProductServiceClient : IProductServiceClient
{
    private readonly HttpClient _client;

    public ProductServiceClient(HttpClient client)
    {
        _client = client;
    }

    public async Task DeactivateProductsByUserIdAsync(string userId, string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PutAsync($"api/products/deactivate/{userId}", new StringContent(""));
        response.EnsureSuccessStatusCode();
    }

    public async Task ActivateProductsByUserIdAsync(string userId, string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PutAsync($"api/products/activate/{userId}", new StringContent(""));
        response.EnsureSuccessStatusCode();
    }
}