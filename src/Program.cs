using AccountService.Application.DTOs;
using AccountService.Application.Interfaces;
using AccountService.Application.Services;
using AccountService.Infrastructure.Email;
using AccountService.Infrastructure.Google;
using AccountService.Infrastructure.Otp;
using AccountService.Infrastructure.Persistence.Repositories;
using AccountService.Infrastructure.Repositories;
using CartService.Application.Interfaces;
using CartService.Infrastructure.Repositories;
using CloudinaryDotNet;
using CourseService.Application.Interfaces;
using CourseService.Application.Services;
using CourseService.Infrastructure.Repositories;
using Data.Context;
using Data.Seeding;
using DotNetEnv;
using Entities;
using LectureService.Application.Interfaces;
using LectureService.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PaymentService.Application.Interfaces;
using PaymentService.Infrastructure.Repositories;
using PaymentService.Infrastructure.Services;
using Serilog;
using Shared.Infrastructure.cloudinaryService;
using src.Shared.Domain.Entities;
using CourseService.Infrastructure;
using Swashbuckle.AspNetCore.SwaggerGen;
using Hangfire;
using Shared.Infrastructure.Hubs;
using Hangfire.Redis.StackExchange;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json.Serialization;
using StackExchange.Redis;
using Hangfire.Dashboard;

const string BearerScheme = "Bearer";
const string AdminRole = "Admin";
const string StudentRole = "Student";
const string InstructorRole = "Instructor";
const string NotificationHubPath = "/notificationHub";

try
{
    Env.Load();
}
catch (Exception ex)
{
    Console.WriteLine($"Error loading environment variables: {ex.Message}");
    throw;
}

var builder = WebApplication.CreateBuilder(args);

var jwtSection = builder.Configuration.GetSection("JWT");
if (string.IsNullOrEmpty(jwtSection["Issuer"]) ||
    string.IsNullOrEmpty(jwtSection["Audience"]) ||
    string.IsNullOrEmpty(jwtSection["SigningKey"]))
{
    throw new InvalidOperationException("JWT configuration is incomplete. Please check JWT:Issuer, JWT:Audience, and JWT:SigningKey in configuration.");
}

builder.Services.AddSignalR();
ConfigureControllers(builder.Services);
ConfigureCache(builder.Services, builder.Configuration);
ConfigureDI(builder.Services, builder.Configuration);
ConfigureLocalization(builder.Services, builder.Configuration);
ConfigureSwagger(builder.Services);
ConfigureIdentity(builder.Services);
ConfigureAuthentication(builder.Services, builder.Configuration);
ConfigureAuthorization(builder.Services);
ConfigureDbContext(builder.Services, builder.Configuration);
ConfigureHangfire(builder.Services, builder.Configuration);

var app = builder.Build();

// using (var scope = app.Services.CreateScope())
// {
//     var services = scope.ServiceProvider;
//     try
//     {
//         var seeder = services.GetRequiredService<DbSeeder>();
//         await seeder.SeedAsync();
//     }
//     catch (Exception ex)
//     {
//         var logger = services.GetRequiredService<ILogger<Program>>();
//         logger.LogError(ex, "An error occurred during database seeding.");
//     }
// }

ConfigureMiddleware(app);

app.Run();

static void ConfigureCache(IServiceCollection services, IConfiguration configuration)
{
    string? redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION");

    if (string.IsNullOrEmpty(redisConnectionString))
    {
        throw new InvalidOperationException("REDIS_CONNECTION environment variable is not set.");
    }

    services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));

    services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "Vietedu_APIcache:";
    });
}

static void ConfigureDI(IServiceCollection services, IConfiguration configuration)
{
    services.AddHttpClient();
    services.AddHttpClient<IAiService, LmsAiService>(client =>
    {
        client.Timeout = TimeSpan.FromMinutes(15);
    });
    services.AddSingleton(provider => new Cloudinary(Environment.GetEnvironmentVariable("CLOUDINARY_URL")));

    services.Configure<GoogleConfig>(configuration.GetSection("Google"));
    services.AddScoped<IGoogleAuthService, GoogleAuthService>();

    services.AddScoped<DbSeeder>();
    services.AddScoped<CloudinaryService>();
    services.AddSingleton<ILuceneSearchService, LuceneSearchService>();
    services.AddScoped<ISepayService, SepayService>();
    services.AddScoped<IVnPayService, VnPayService>();
    services.AddScoped<IPaymentRepository, PaymentRepository>();
    services.AddScoped<IPaymentService, PaymentService.Application.Services.PaymentService>();
    services.AddScoped<ICourseRepository, CourseRepository>();
    services.AddScoped<ILectureRepository, LectureRepository>();
    services.AddScoped<IQuizRepository, QuizRepository>();
    services.AddScoped<ICartRepository, CartRepository>();
    services.AddScoped<ITagRepository, TagRepository>();
    services.AddScoped<ITokenService, TokenService>();
    services.AddScoped<IAccountRepository, AccountRepository>();
    services.AddScoped<DashboardRepository>();
    services.AddScoped<IDashboardRepository>(provider =>
        new CachedDashboardRepository(provider.GetRequiredService<DashboardRepository>(), provider.GetRequiredService<IDistributedCache>()));
    services.AddScoped<IAuthService, AuthService>();
    services.AddScoped<IEmailService, EmailService>();
    services.AddScoped<IOtpService, OtpService>();
    services.AddScoped<INotificationRepository, NotificationRepository>();
    services.AddScoped<IVideoProcessingService, VideoProcessingService>();
}

static void ConfigureHangfire(IServiceCollection services, IConfiguration configuration)
{
    string? redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION");
    if (string.IsNullOrEmpty(redisConnectionString))
    {
        throw new InvalidOperationException("REDIS_CONNECTION environment variable is not set.");
    }

    services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseRedisStorage(redisConnectionString));

    services.AddHangfireServer(options =>
    {
        options.ServerName = "CartProcessor";
        options.WorkerCount = 5;
        options.Queues = new[] { "critical" };
    });

    services.AddHangfireServer(options =>
    {
        options.ServerName = "VideoProcessor";
        options.WorkerCount = 2;
        options.Queues = new[] { "video" };
    });
}

static void ConfigureLocalization(IServiceCollection services, IConfiguration configuration)
{
    services.AddLocalization(options => options.ResourcesPath = "");
    services.Configure<RequestLocalizationOptions>(options =>
    {
        var supportedCultures = new[] { "vi", "en" };
        options.SetDefaultCulture("vi")
            .AddSupportedCultures(supportedCultures)
            .AddSupportedUICultures(supportedCultures);

        options.RequestCultureProviders = new List<IRequestCultureProvider>
        {
            new AcceptLanguageHeaderRequestCultureProvider(),
            new QueryStringRequestCultureProvider(),
            new CookieRequestCultureProvider()
        };
    });
}

static void ConfigureDbContext(IServiceCollection services, IConfiguration configuration)
{
    string? ConnectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION");
    services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(ConnectionString));
}

static void ConfigureControllers(IServiceCollection services)
{
    services.AddControllers().AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;

        options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
    });
    services.AddEndpointsApiExplorer();
}

static void ConfigureSwagger(IServiceCollection services)
{
    services.AddSwaggerGen(option =>
    {
        option.SwaggerDoc("v1", new OpenApiInfo { Title = "Demo API", Version = "v1" });
        option.CustomSchemaIds(type => type.FullName);
        option.AddSecurityDefinition(BearerScheme, new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "Please enter a valid token",
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            BearerFormat = "JWT",
            Scheme = BearerScheme
        });
        option.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = BearerScheme
                    }
                },
                Array.Empty<string>()
            }
        });
        option.OperationFilter<AcceptLanguageHeaderOperationFilter>();
        option.SchemaFilter<LocalizedStringSchemaFilter>();
    });
}

static void ConfigureIdentity(IServiceCollection services)
{
    services.AddIdentity<User, IdentityRole>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();
}

static void ConfigureAuthentication(IServiceCollection services, IConfiguration configuration)
{
    services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
        options.DefaultChallengeScheme =
        options.DefaultForbidScheme =
        options.DefaultScheme =
        options.DefaultSignInScheme =
        options.DefaultSignOutScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = configuration["JWT:Issuer"],
            ValidateAudience = true,
            ValidAudience = configuration["JWT:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT__SigningKey") ?? configuration["JWT:SigningKey"] ?? string.Empty)
            ),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments(NotificationHubPath))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
}

static void ConfigureAuthorization(IServiceCollection services)
{
    services.AddAuthorization(options =>
    {
        options.AddPolicy(AdminRole, p => p.RequireRole(AdminRole));
        options.AddPolicy(StudentRole, p => p.RequireRole(StudentRole));
        options.AddPolicy(InstructorRole, p => p.RequireRole(InstructorRole));
    });
}

static void ConfigureMiddleware(WebApplication app)
{
    if (app.Environment.IsDevelopment() || Environment.GetEnvironmentVariable("ENABLE_SWAGGER") == "true")
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Demo API V1");
            c.RoutePrefix = "swagger";
        });
    }

    app.UseRequestLocalization();
    app.UseRouting();

    app.UseCors(x => x
        .WithOrigins("https://vietedu.id.vn")
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new MyHangfireAuthorizationFilter() }
    });

    app.MapControllers();
    app.MapHub<NotificationHub>(NotificationHubPath);
}

public class AcceptLanguageHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= new List<OpenApiParameter>();
        var existingParam = operation.Parameters.FirstOrDefault(p => p.Name == "Accept-Language");
        if (existingParam != null) operation.Parameters.Remove(existingParam);

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "Accept-Language",
            In = ParameterLocation.Header,
            Description = "(vi = Tiếng Việt, en = English)",
            Required = false,
            Schema = new OpenApiSchema
            {
                Type = "string",
                Default = new Microsoft.OpenApi.Any.OpenApiString("vi"),
                Enum = new List<Microsoft.OpenApi.Any.IOpenApiAny>
                {
                    new Microsoft.OpenApi.Any.OpenApiString("vi"),
                    new Microsoft.OpenApi.Any.OpenApiString("en")
                }
            }
        });
    }
}

public class LocalizedStringSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(ApiResponse))
        {
            schema.Example = new Microsoft.OpenApi.Any.OpenApiObject
            {
                ["success"] = new Microsoft.OpenApi.Any.OpenApiBoolean(true),
                ["code"] = new Microsoft.OpenApi.Any.OpenApiString("SUCCESS"),
                ["message"] = new Microsoft.OpenApi.Any.OpenApiString("Thành công / Success"),
                ["data"] = new Microsoft.OpenApi.Any.OpenApiNull()
            };
            Console.WriteLine("Applied LocalizedStringSchemaFilter to ApiResponse");
        }
    }
}

public class MyHangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        return true;
    }
}
