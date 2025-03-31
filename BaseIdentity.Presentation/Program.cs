using BaseIdentity.Application.Interface.IServices;
using BaseIdentity.Application.Interface.IToken;
using BaseIdentity.Application.Interface.Repositories.IGenericRepository;
using BaseIdentity.Application.Interface.Repositories.IUnitOfWork;
using BaseIdentity.Application.Mapping;
using BaseIdentity.Application.Services;
using BaseIdentity.Infrastructure.Data.UnitOfWork;
using BaseIdentity.Infrastructure.DependencyInjection;
using BaseIdentity.Infrastructure.Repositories.GenericRepository;
using BaseIdentity.Presentation.Exceptions;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Thêm các dịch vụ vào container.

builder.Services.AddHttpContextAccessor();
builder.Services.AddProblemDetails();


// Cấu hình các dịch vụ của bạn
builder.Services.AddInfrastructureServices(builder.Configuration);

// Đăng ký các dịch vụ cụ thể
builder.Services.AddScoped<ITokenServices, TokenServices>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);
builder.Services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IUserServices, UserServices>();

// Cấu hình xác thực và phân quyền
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Adding Swagger
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
        Description = "Please enter a valid token in the following format: {your token here} do not add the word 'Bearer' before it."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});





var app = builder.Build();
app.UseCors("CorsPolicy");
// Cấu hình pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalExceptionMiddleware>();


app.UseHttpsRedirection();

// Thêm xác thực và phân quyền
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
