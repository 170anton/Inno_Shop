using Microsoft.EntityFrameworkCore;
using ProductService.Infrastructure.Data;
using ProductService.Application.Interfaces;
using ProductService.Application.Services;
using ProductService.Infrastructure.Repositories;
using ProductService.Domain.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using ProductService.Application.Validators;
using System.IdentityModel.Tokens.Jwt;
using MediatR;
using ProductService.Application.Commands;
using ProductService.Infrastructure.Behaviors;

var builder = WebApplication.CreateBuilder(args);
var env = builder.Environment;

if (env.IsEnvironment("IntegrationTests"))
    builder.Services.AddDbContext<ProductDbContext>(opts =>
        opts.UseInMemoryDatabase("InMemoryTestDb"));
else
    builder.Services.AddDbContext<ProductDbContext>(opts =>
        opts.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddMediatR(typeof(CreateProductCommand).Assembly);
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductCommandValidator>();
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService.Application.Services.ProductService>();


JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtSettings = builder.Configuration.GetSection("Jwt");
    var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured");
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "ProductService API",
        Version = "v1"
    });
    
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Enter your JWT token without the 'Bearer ' prefix.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});


builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();



var app = builder.Build();

app.UseMiddleware<ProductService.API.Middleware.GlobalExceptionMiddleware>();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

    if (env.IsEnvironment("IntegrationTests"))
        db.Database.EnsureCreated();
    else
        db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ProductService.API.Middleware.GlobalExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
