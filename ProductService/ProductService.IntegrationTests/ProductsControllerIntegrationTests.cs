using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ProductService.API;
using ProductService.Application.DTOs;
using ProductService.Domain.Entities;
using ProductService.Infrastructure.Data;
using Xunit;

namespace ProductService.IntegrationTests
{
    public class ProductsControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _defaultClient;
        private readonly CustomWebApplicationFactory _factory;
        private readonly Guid _testUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        public ProductsControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _defaultClient = factory.CreateClient();
            _defaultClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Test");
        }

        private HttpClient CreateClient(string? testUserId = null)
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
            if (testUserId != null)
                client.DefaultRequestHeaders.Add("Test-UserId", testUserId);
            return client;
        }

        [Fact]
        public async Task GetAll_WithProductsForUser_ReturnsOnlyOwn()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();

                db.Products.Add(new Product { Name="A", Price=1, IsAvailable=true, CreatedByUserId=_testUserId });
                db.Products.Add(new Product { Name="B", Price=2, IsAvailable=true, CreatedByUserId=Guid.NewGuid() });
                await db.SaveChangesAsync();
            }

            var client = CreateClient(_testUserId.ToString());
            var resp   = await client.GetAsync("/api/products");

            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            var list = await resp.Content.ReadFromJsonAsync<List<Product>>();
            Assert.Single(list);
            Assert.Equal("A", list![0].Name);
        }

        // [Fact]
        // public async Task GetAll_WithoutAuthHeader_ReturnsUnauthorized()
        // {
        //     var client = _factory.CreateClient(); 
        //     var resp   = await client.GetAsync("/api/products");
        //     Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        // }

        [Fact]
        public async Task GetById_OwnedProduct_ReturnsOk()
        {
            var createDto  = new CreateProductDto { Name = "X", Description = "Y", Price = 1m, IsAvailable = true };
            var createResp = await _defaultClient.PostAsJsonAsync("/api/products", createDto);
            createResp.EnsureSuccessStatusCode();
            var created = await createResp.Content.ReadFromJsonAsync<Product>();

            var getResp = await _defaultClient.GetAsync($"/api/products/{created!.Id}");
            Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
        }

        [Fact]
        public async Task GetById_NotOwnedProduct_ReturnsForbid()
        {
            Guid id;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
                var p  = new Product { Name = "Y", Price = 5, IsAvailable = true, CreatedByUserId = Guid.NewGuid() };
                db.Products.Add(p);
                await db.SaveChangesAsync();
                id = p.Id;
            }

            var client = CreateClient(_testUserId.ToString());
            var resp   = await client.GetAsync($"/api/products/{id}");
            Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        }

        [Fact]
        public async Task Create_ValidDto_ReturnsCreated()
        {
            var client = CreateClient(_testUserId.ToString());
            var dto    = new CreateProductDto { Name = "New", Description = "Desc", Price = 9, IsAvailable = true };

            var resp    = await client.PostAsJsonAsync("/api/products", dto);
            Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

            var created = await resp.Content.ReadFromJsonAsync<Product>();
            Assert.Equal("New", created!.Name);
        }

        // [Fact]
        // public async Task Create_WithoutAuthHeader_ReturnsUnauthorized()
        // {
        //     var client = _factory.CreateClient();
        //     var dto    = new CreateProductDto { Name = "No", Description = "No", Price = 0, IsAvailable = true };

        //     var resp = await client.PostAsJsonAsync("/api/products", dto);
        //     Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        // }

        [Fact]
        public async Task Update_Existing_ReturnsNoContent()
        {
            Guid id;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
                var p  = new Product { Name = "Up1", Price = 1, IsAvailable = true, CreatedByUserId = _testUserId };
                db.Products.Add(p);
                await db.SaveChangesAsync();
                id = p.Id;
            }

            var client = CreateClient(_testUserId.ToString());
            var dto    = new UpdateProductDto { Name = "Up2" };
            var resp   = await client.PutAsJsonAsync($"/api/products/{id}", dto);

            Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
        }

        [Fact]
        public async Task Update_NotFound_ReturnsNotFound()
        {
            var client = CreateClient(_testUserId.ToString());
            var dto    = new UpdateProductDto { Name = "X" };
            var resp   = await client.PutAsJsonAsync($"/api/products/{Guid.NewGuid()}", dto);
            Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        }

        [Fact]
        public async Task Delete_Existing_ReturnsNoContent()
        {
            Guid id;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
                var p  = new Product { Name = "D1", Price = 1, IsAvailable = true, CreatedByUserId = _testUserId };
                db.Products.Add(p);
                await db.SaveChangesAsync();
                id = p.Id;
            }

            var client = CreateClient(_testUserId.ToString());
            var resp   = await client.DeleteAsync($"/api/products/{id}");

            Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
        }

        [Fact]
        public async Task Delete_NotOwned_ReturnsForbid()
        {
            Guid id;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
                var p  = new Product { Name = "D2", Price = 2, IsAvailable = true, CreatedByUserId = Guid.NewGuid() };
                db.Products.Add(p);
                await db.SaveChangesAsync();
                id = p.Id;
            }

            var client = CreateClient(_testUserId.ToString());
            var resp   = await client.DeleteAsync($"/api/products/{id}");

            Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        }

        [Fact]
        public async Task DeactivateProductsByUserId_AnyUser_ReturnsNoContent()
        {
            var client = CreateClient(_testUserId.ToString());
            var resp   = await client.PutAsync($"/api/products/deactivate/{_testUserId}", null);
            Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
        }

        // [Fact]
        // public async Task DeactivateProductsByUserId_NoAuth_ReturnsUnauthorized()
        // {
        //     var client = _factory.CreateClient();
        //     var resp   = await client.PutAsync($"/api/products/deactivate/{_testUserId}", null);
        //     Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        // }

        [Fact]
        public async Task ActivateProductsByUserId_AnyUser_ReturnsNoContent()
        {
            var client = CreateClient(_testUserId.ToString());
            var resp   = await client.PutAsync($"/api/products/activate/{_testUserId}", null);
            Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
        }

        // [Fact]
        // public async Task ActivateProductsByUserId_NoAuth_ReturnsUnauthorized()
        // {
        //     var client = _factory.CreateClient();
        //     var resp   = await client.PutAsync($"/api/products/activate/{_testUserId}", null);
        //     Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        // }

        [Fact]
        public async Task SearchProducts_ValidCriteria_ReturnsOk()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
                db.Products.Add(new Product { Name = "foo", Price = 5, IsAvailable = true, CreatedByUserId = _testUserId });
                db.Products.Add(new Product { Name = "bar", Price = 10, IsAvailable = true, CreatedByUserId = _testUserId });
                await db.SaveChangesAsync();
            }

            var client = CreateClient(_testUserId.ToString());
            var resp   = await client.GetAsync("/api/products/search?name=ba");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            var list = await resp.Content.ReadFromJsonAsync<List<Product>>();
            Assert.Single(list);
            Assert.Equal("bar", list![0].Name);
        }

        // [Fact]
        // public async Task SearchProducts_NoAuth_ReturnsUnauthorized()
        // {
        //     var client = _factory.CreateClient();
        //     var resp   = await client.GetAsync("/api/products/search?name=anything");
        //     Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        // }
    }
}
