using IdentityService.Domain.Entities;
using IdentityService.Application.DTOs;
using IdentityService.Application.Interfaces;
using IdentityService.Application.Services;
using IdentityService.Infrastructure.Email;
using IdentityService.Infrastructure.Google;
using IdentityService.Infrastructure.Otp;
using IdentityService.Infrastructure.Repositories;
using IdentityService.Infrastructure.Repositories;
using OrderingService.Application.Interfaces;
using OrderingService.Infrastructure.Repositories;
using CloudinaryDotNet;
using ContentService.Application.Interfaces;
using ContentService.Application.Services;
using ContentService.Infrastructure.Repositories;
using Data.Context;
using Data.Seeding;
using DotNetEnv;
using ContentService.Application.Interfaces;
using ContentService.Infrastructure.Repositories;
using InteractionService.Application.Interfaces;
using InteractionService.Application.Services;
using InteractionService.Infrastructure.Repositories;
using LearningService.Application.Interfaces;
using LearningService.Application.Services;
using SearchService.Application.Interfaces;
using SearchService.Application.Services;
using SearchService.Infrastructure;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Services;
using NotificationService.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OrderingService.Application.Interfaces;
using OrderingService.Infrastructure.Repositories;
using OrderingService.Infrastructure.Services;
using Serilog;
using Shared.Infrastructure.cloudinaryService;
using src.Shared.Domain.Entities;
using SearchService.Infrastructure;
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

    var options = ConfigurationOptions.Parse(redisConnectionString);
    options.AbortOnConnectFail = false; // Quan trọng: Không crash app nếu chưa thấy Redis ngay lập tức
    options.ConnectTimeout = 10000;      // Tăng timeout lên 10s
    options.ConnectRetry = 5;            // Thử lại 5 lần

    var multiplexer = ConnectionMultiplexer.Connect(options);
    services.AddSingleton<IConnectionMultiplexer>(multiplexer);

    services.AddStackExchangeRedisCache(opt =>
    {
        opt.ConfigurationOptions = options; // Dùng chung options đã cấu hình
        opt.InstanceName = "Vietedu_APIcache:";
    });
}

static void ConfigureDI(IServiceCollection services, IConfiguration configuration)
{
    services.AddHttpClient();
    // --- Search Service ---
    services.AddHttpClient<IAiService, LmsAiService>(client =>
    {
        client.Timeout = TimeSpan.FromMinutes(15);
    });
    services.AddSingleton<ILuceneSearchService>(provider =>
    {
        var env = provider.GetRequiredService<IWebHostEnvironment>();
        var baseDataPath = Path.Combine(env.ContentRootPath, "lucene_data");
        var spellcheckerPath = Path.Combine(baseDataPath, "spellchecker");

        if (!Directory.Exists(baseDataPath)) Directory.CreateDirectory(baseDataPath);
        if (!Directory.Exists(spellcheckerPath)) Directory.CreateDirectory(spellcheckerPath);

        return ActivatorUtilities.CreateInstance<LuceneSearchService>(provider);
    });
    services.AddScoped<IVideoProcessingService, VideoProcessingService>();

    // --- Content Service ---
    services.AddSingleton(provider => new Cloudinary(Environment.GetEnvironmentVariable("CLOUDINARY_URL")));
    services.AddScoped<CloudinaryService>();
    services.AddScoped<ICourseRepository, CourseRepository>();
    services.AddScoped<ITagRepository, TagRepository>();
    services.AddScoped<ICourseService, ContentService.Application.Services.CourseService>();
    services.AddScoped<IInstructorService, ContentService.Application.Services.InstructorService>();
    services.AddScoped<ContentService.Application.Interfaces.ILectureService, ContentService.Application.Services.LectureService>();
    services.AddScoped<ContentService.Application.Interfaces.ILectureRepository, ContentService.Infrastructure.Repositories.LectureRepository>();
    services.AddScoped<ContentService.Application.Interfaces.IQuizService, ContentService.Application.Services.QuizService>();
    services.AddScoped<ContentService.Application.Interfaces.IQuizRepository, ContentService.Infrastructure.Repositories.QuizRepository>();

    // --- Learning Service ---
    services.AddScoped<IStudentProgressService, StudentProgressService>();

    // --- Interaction Service ---
    services.AddScoped<IQAThreadRepository, QAThreadRepository>();
    services.AddScoped<IWishlistRepository, WishlistRepository>();
    services.AddScoped<ICommentRepository, CommentRepository>();
    services.AddScoped<IQAThreadService, QAThreadService>();
    services.AddScoped<IWishlistService, WishlistService>();
    services.AddScoped<ICommentService, CommentService>();

    // --- Ordering Service ---
    services.AddScoped<ISepayService, SepayService>();
    services.AddScoped<IVnPayService, VnPayService>();
    services.AddScoped<IPaymentRepository, PaymentRepository>();
    services.AddScoped<IPaymentService, OrderingService.Application.Services.PaymentService>();
    services.AddScoped<ICartRepository, CartRepository>();

    // --- Identity Service ---
    services.Configure<GoogleConfig>(configuration.GetSection("Google"));
    services.AddScoped<IGoogleAuthService, GoogleAuthService>();
    services.AddScoped<ITokenService, TokenService>();
    services.AddScoped<IAccountRepository, AccountRepository>();
    services.AddScoped<IdentityService.Application.Interfaces.IUserService, IdentityService.Application.Services.UserService>();
    services.AddScoped<IdentityService.Application.Interfaces.IDashboardService, IdentityService.Application.Services.DashboardService>();
    services.AddScoped<IDashboardRepository, DashboardRepository>();
    services.AddScoped<IDashboardRepository>(provider =>
        new CachedDashboardRepository(provider.GetRequiredService<DashboardRepository>(), provider.GetRequiredService<IDistributedCache>()));
    services.AddScoped<IAuthService, AuthService>();
    services.AddScoped<IEmailService, EmailService>();
    services.AddScoped<IOtpService, OtpService>();
    services.AddScoped<DbSeeder>();

    // --- Notification Service ---
    services.AddScoped<NotificationService.Application.Interfaces.INotificationService, NotificationService.Application.Services.NotificationService>();
    services.AddScoped<INotificationRepository, NotificationRepository>();
}

static void ConfigureHangfire(IServiceCollection services, IConfiguration configuration)
{
    string? redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION");
    if (string.IsNullOrEmpty(redisConnectionString))
    {
        throw new InvalidOperationException("REDIS_CONNECTION environment variable is not set.");
    }

    // Thiết lập options chuẩn cho Redis
    var options = ConfigurationOptions.Parse(redisConnectionString);
    options.AbortOnConnectFail = false; 
    options.ConnectTimeout = 10000;
    options.SyncTimeout = 10000;

    services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        // Sử dụng options.ToString() để đảm bảo chuỗi kết nối luôn chuẩn (bao gồm cả abortConnect=false)
        .UseRedisStorage(options.ToString(), new RedisStorageOptions
        {
            Prefix = "hangfire:",
            InvisibilityTimeout = TimeSpan.FromMinutes(5)
        }));

    services.AddHangfireServer(options =>
    {
        options.ServerName = "MainProcessor";
        options.WorkerCount = 4; // Tăng worker để xử lý song song tốt hơn
        options.Queues = new[] { "video", "default" };
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
    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    
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

        options.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;
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
    app.UseMiddleware<Shared.Application.Middlewares.GlobalExceptionMiddleware>();

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

