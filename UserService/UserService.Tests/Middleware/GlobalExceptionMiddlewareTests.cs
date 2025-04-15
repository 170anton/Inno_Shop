using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using UserService.API.Middleware;
using Xunit;

namespace UserService.Tests.Middleware
{
    public class GlobalExceptionMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_ExceptionThrown_ReturnsProblemDetailsWith500()
        {
            RequestDelegate next = (HttpContext context) => throw new Exception("Test Exception");

            var loggerMock = new Mock<ILogger<GlobalExceptionMiddleware>>();
            var middleware = new GlobalExceptionMiddleware(next, loggerMock.Object);

            var context = new DefaultHttpContext();
            var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await middleware.InvokeAsync(context);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            string responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();


            Assert.Equal(500, context.Response.StatusCode);


            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(problemDetails);
            Assert.Equal(500, problemDetails.Status);
            Assert.Equal("An unexpected error occurred.", problemDetails.Title);
        }
    }
}
