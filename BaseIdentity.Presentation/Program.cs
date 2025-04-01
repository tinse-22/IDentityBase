using BaseIdentity.Application.Mapping;
using BaseIdentity.Application.Services;
using BaseIdentity.Infrastructure.Data.UnitOfWork;
using BaseIdentity.Infrastructure.DependencyInjection;
using BaseIdentity.Infrastructure.Repositories.GenericRepository;
using BaseIdentity.Presentation.Exceptions;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using BaseIdentity.Application.Interface.IServices;
using BaseIdentity.Application.Interface.IToken;
using BaseIdentity.Application.Interface.Repositories.IGenericRepository;
using BaseIdentity.Application.Interface.Repositories.IUnitOfWork;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký HttpContextAccessor và ProblemDetails
builder.Services.AddHttpContextAccessor();
builder.Services.AddProblemDetails();

// Cấu hình DB, Identity, JWT, CORS (được định nghĩa trong Infrastructure)
builder.Services.AddInfrastructureServices(builder.Configuration);
// Cấu hình AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);

// Cấu hình Google OAuth (cho phép đăng nhập bằng Google)
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
        // options.CallbackPath = "/signin-google"; // Nếu muốn custom callback
    });

// Cấu hình Controllers và Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "User Auth", Version = "v1", Description = "Services to Authenticate user" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your token (no 'Bearer' prefix)."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, new List<string>() }
    });
});
var app = builder.Build();

// Pipeline cấu hình
app.UseCors("CorsPolicy");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
