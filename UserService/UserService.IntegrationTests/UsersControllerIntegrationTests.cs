using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Xunit;
using UserService.API;
using UserService.Application.DTOs;
using UserService.Domain.Entities;
using System.Net.Http.Headers;

namespace UserService.IntegrationTests
{
    public class UsersControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;

        public UsersControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client  = factory.CreateClient();
        }

        private async Task<string> RegisterNewUserAsync()
        {
            var email = $"{Guid.NewGuid():N}@example.com";
            var reg = new RegisterModel {
                Email    = email,
                Password = "Password!1",
                Name     = "TestUser",
                Address  = "Addr"
            };
            var resp = await _client.PostAsJsonAsync("/api/auth/register", reg);
            resp.EnsureSuccessStatusCode();

            using var scope = _factory.Services.CreateScope();
            var um = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = await um.FindByEmailAsync(email);
            return user!.Id;
        }


        [Fact]
        public async Task GetAll_WithValidToken_ReturnsOk()
        {
            var userId = await RegisterNewUserAsync();
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", userId);

            var resp = await _client.GetAsync("/api/users");
            resp.EnsureSuccessStatusCode();

            using var arr = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());


            Assert.Equal(JsonValueKind.Array, arr.RootElement.ValueKind);
            Assert.Contains(arr.RootElement.EnumerateArray(),
                e => e.GetProperty("id").GetString() == userId);
        }

        [Fact]
        public async Task GetAll_WithoutToken_ReturnsUnauthorized()
        {
            var resp = await _client.GetAsync("/api/users");


            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }


        [Fact]
        public async Task GetById_Existing_ReturnsOk()
        {
            var userId = await RegisterNewUserAsync();
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", userId);

            var resp = await _client.GetAsync($"/api/users/{userId}");
            resp.EnsureSuccessStatusCode();

            using var obj = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());


            Assert.Equal(userId, obj.RootElement.GetProperty("id").GetString());
        }

        [Fact]
        public async Task GetById_NotFound_ReturnsNotFound()
        {
            var userId = await RegisterNewUserAsync();
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", userId);

            var resp = await _client.GetAsync($"/api/users/{Guid.NewGuid():N}");


            Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        }
        

        [Fact]
        public async Task Update_Existing_ReturnsOkAndUpdates()
        {
            var userId = await RegisterNewUserAsync();
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", userId);

            var update = new UpdateUserModel {
                Name    = "NewName",
                Address = "NewAddr"
            };
            var resp = await _client.PutAsJsonAsync($"/api/users/{userId}", update);
            resp.EnsureSuccessStatusCode();

            using var obj = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());


            Assert.Equal(update.Name, obj.RootElement.GetProperty("name").GetString());
            Assert.Equal(update.Address, obj.RootElement.GetProperty("address").GetString());
        }

        [Fact]
        public async Task Update_NotFound_ReturnsNotFound()
        {
            var userId = await RegisterNewUserAsync();
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", userId);

            var resp = await _client.PutAsJsonAsync(
                $"/api/users/{Guid.NewGuid():N}",
                new UpdateUserModel { Name = "X" });


            Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        }
        

        [Fact]
        public async Task Delete_Existing_ReturnsNoContent()
        {
            var userId = await RegisterNewUserAsync();
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", userId);

            var resp = await _client.DeleteAsync($"/api/users/{userId}");


            Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
        }

        [Fact]
        public async Task Delete_NotFound_ReturnsBadRequest()
        {
            var userId = await RegisterNewUserAsync();
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", userId);

            var resp = await _client.DeleteAsync($"/api/users/{Guid.NewGuid():N}");


            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }


        [Fact]
        public async Task Deactivate_WithToken_ReturnsOkAndFlagsFalse()
        {
            var userId = await RegisterNewUserAsync();
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", userId);

            var resp = await _client.PutAsync($"/api/users/{userId}/deactivate", null);
            resp.EnsureSuccessStatusCode();

            using var obj = JsonDocument.Parse(
                await _client.GetAsync($"/api/users/{userId}")
                             .Result.Content.ReadAsStringAsync()
            );


            Assert.False(obj.RootElement.GetProperty("isActivated").GetBoolean());
        }

        [Fact]
        public async Task Deactivate_NoToken_ReturnsUnauthorized()
        {
            var userId = await RegisterNewUserAsync();
            var resp   = await _client.PutAsync($"/api/users/{userId}/deactivate", null);


            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }


        [Fact]
        public async Task Activate_WithToken_ReturnsOkAndFlagsTrue()
        {
            var userId = await RegisterNewUserAsync();
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", userId);

            await _client.PutAsync($"/api/users/{userId}/deactivate", null);

            var resp = await _client.PutAsync($"/api/users/{userId}/activate", null);
            resp.EnsureSuccessStatusCode();

            using var obj = JsonDocument.Parse(
                await _client.GetAsync($"/api/users/{userId}")
                             .Result.Content.ReadAsStringAsync()
            );


            Assert.True(obj.RootElement.GetProperty("isActivated").GetBoolean());
        }

        [Fact]
        public async Task Activate_NoToken_ReturnsUnauthorized()
        {
            var userId = await RegisterNewUserAsync();
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", userId);

            await _client.PutAsync($"/api/users/{userId}/deactivate", null);
            _client.DefaultRequestHeaders.Authorization = null;

            var resp = await _client.PutAsync($"/api/users/{userId}/activate", null);
            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }
    }
}
