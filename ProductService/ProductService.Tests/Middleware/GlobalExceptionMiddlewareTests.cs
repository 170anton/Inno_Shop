using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ProductService.API.Middleware;

namespace ProductService.Tests.Middleware
{
    public class GlobalExceptionMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_NoException_PassesThroughResponseUnchanged()
        {
            var context = new DefaultHttpContext();
            var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            RequestDelegate next = async (ctx) =>
            {
                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsync("Hello");
            };

            var loggerMock = new Mock<ILogger<GlobalExceptionMiddleware>>();
            var middleware = new GlobalExceptionMiddleware(next, loggerMock.Object);

            await middleware.InvokeAsync(context);


            context.Response.Body.Seek(0, SeekOrigin.Begin);
            string responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();


            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal("Hello", responseText);
        }

        [Fact]
        public async Task InvokeAsync_WithException_Returns500AndProblemDetails()
        {
            var context = new DefaultHttpContext();
            var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            var exceptionMessage = "Test exception";
            RequestDelegate next = (ctx) => throw new Exception(exceptionMessage);

            var loggerMock = new Mock<ILogger<GlobalExceptionMiddleware>>();
            var middleware = new GlobalExceptionMiddleware(next, loggerMock.Object);


            await middleware.InvokeAsync(context);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();


            Assert.Equal(500, context.Response.StatusCode);
            Assert.StartsWith("application/json", context.Response.ContentType);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var problem = JsonSerializer.Deserialize<ProblemDetails>(responseText, options);

            Assert.NotNull(problem);
            Assert.Equal(500, problem.Status);
            Assert.Equal("An unexpected error occurred.", problem.Title);
            Assert.Equal(exceptionMessage, problem.Detail);
        }
    }
}
