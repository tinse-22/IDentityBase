using BaseIdentity.Application.DTOs.Request;
using BaseIdentity.Application.Interface.IExternalAuthService;
using BaseIdentity.Application.Interface.IServices;
using BaseIdentity.Application.Interface.IToken;
using BaseIdentity.Application.Interface.Repositories.IGenericRepository;
using BaseIdentity.Application.Interface.Repositories.IUnitOfWork;
using BaseIdentity.Application.Services;
using BaseIdentity.Domain.Entities;
using BaseIdentity.Infrastructure.Data;
using BaseIdentity.Infrastructure.Data.UnitOfWork;
using BaseIdentity.Infrastructure.Repositories.GenericRepository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace BaseIdentity.Infrastructure.DependencyInjection
{
    public static class InfrastructureServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // 1. Cấu hình DbContext (SQL Server)
            services.AddDbContext<IdentityBaseDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("IdentityAuthentication"),
                    sqlOptions => sqlOptions.MigrationsAssembly(typeof(InfrastructureServiceCollectionExtensions).Assembly.FullName)
                ));

            // 2. Cấu hình CORS
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            // 3. Cấu hình ASP.NET Core Identity
            services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.AllowedForNewUsers = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 8;
            })
            .AddEntityFrameworkStores<IdentityBaseDbContext>()
            .AddDefaultTokenProviders();

            // 4. Cấu hình JWT Authentication
            var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>();
            if (jwtSettings == null || string.IsNullOrEmpty(jwtSettings.Key))
            {
                throw new InvalidOperationException("JWT secret key is not configured.");
            }

            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key));

            services.AddAuthentication(options =>
            {
                // Scheme mặc định
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.ValidIssuer,
                    ValidAudience = jwtSettings.ValidAudience,
                    IssuerSigningKey = secretKey
                };

                // Tuỳ biến phản hồi khi không có token
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        var result = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            message = "You are not authorized to access this resource. Please authenticate."
                        });
                        return context.Response.WriteAsync(result);
                    }
                };
            });

            // 5. Đăng ký thêm các repository / service hạ tầng
            // services.AddScoped<IAccountRepository, AccountRepository>(); v.v.
            services.AddScoped<IExternalAuthService, ExternalAuthService>();
            // Đăng ký các dịch vụ ứng dụng
            services.AddScoped<ITokenServices, TokenServices>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IUserServices, UserServices>();
            services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));


            return services;
        }
    }
}
