using Data.AppDbContext;
using DotNetEnv;
using Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

const string BearerScheme = "Bearer";
const string AdminRole = "Admin";
const string StudentRole = "Student";
const string InstructorRole = "Instructor";
// const string NotificationHubPath = "/notificationHub";

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

ConfigureControllers(builder.Services);
ConfigureSwagger(builder.Services);
ConfigureIdentity(builder.Services);
ConfigureAuthentication(builder.Services, builder.Configuration);
ConfigureAuthorization(builder.Services);
ConfigureDbContext(builder.Services, builder.Configuration);

var app = builder.Build();

ConfigureMiddleware(app);

app.Run();
static void ConfigureDbContext(IServiceCollection services, IConfiguration configuration)
{
    // var ConnectionString = configuration.GetConnectionString("DefaultConnection");
    // if (string.IsNullOrEmpty(connectionString))
    // {
    //     throw new InvalidOperationException("Database connection string 'DefaultConnection' is not configured.");
    // }
    string ConnectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION");

    services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(ConnectionString));
}
static void ConfigureControllers(IServiceCollection services)
{
    services.AddControllers().AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
    services.AddEndpointsApiExplorer();
}

static void ConfigureSwagger(IServiceCollection services)
{
    services.AddSwaggerGen(option =>
    {
        option.SwaggerDoc("v1", new OpenApiInfo { Title = "Demo API", Version = "v1" });
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
    });
}

static void ConfigureIdentity(IServiceCollection services)
{
    services.AddIdentity<User, IdentityRole<int>>()
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
                System.Text.Encoding.UTF8.GetBytes(configuration["JWT:SigningKey"])
            ),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        // options.Events = new JwtBearerEvents
        // {
        //     OnMessageReceived = context =>
        //     {
        //         try
        //         {
        //             var accessToken = context.Request.Query["access_token"];
        //             var path = context.HttpContext.Request.Path;
        //             if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments(NotificationHubPath))
        //             {
        //                 context.Token = accessToken;
        //             }
        //         }
        //         catch (Exception ex)
        //         {
        //             Console.WriteLine($"Error in JWT OnMessageReceived: {ex.Message}");
        //         }
        //         return Task.CompletedTask;
        //     },
        //     OnAuthenticationFailed = context =>
        //     {
        //         Console.WriteLine($"JWT Authentication failed: {context.Exception.Message}");
        //         return Task.CompletedTask;
        //     }
        // };
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
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors(x => x
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials()
        .SetIsOriginAllowed(origin => true));

    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
}
