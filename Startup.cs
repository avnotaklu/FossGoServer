
using System.Text;
using BadukServer;
using BadukServer.Hubs;
using BadukServer.Models;
using BadukServer.Services;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
public class Startup
{
    // var builder = WebApplication.CreateBuilder(args);
    // var services = builder.Services;
    // var config = builder.Configuration;
    // Console.WriteLine("Startup: ");
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddControllers();

        services.AddSingleton<AuthenticationService>();
        services.AddSingleton<UsersService>();
        services.AddSingleton<IDateTimeService, DateTimeService>();
        services.AddSingleton<ISignalRGameHubService, SignalRGameHubService>();

        services.AddEndpointsApiExplorer();

        services.Configure<DatabaseSettings>(
            Configuration.GetSection("UserDatabase"));

        services.Configure<MongodbCollectionParams<User>>(
            Configuration.GetSection("UserCollection"));

        services.Configure<JwtSettings>(
            Configuration.GetSection("JwtSettings"));

        services.AddAuthentication(x =>
        {
            x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(x =>
            x.TokenValidationParameters = new TokenValidationParameters
            {
                // ValidIssuer = config["JwtSettings:Issuer"],
                // ValidAudience = config["JwtSettings:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey
        (Encoding.UTF8.GetBytes(Configuration["JwtSettings:Key"]!)),
                ValidateIssuer = false,
                ValidateAudience
        = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true
            });
        // services.AddHostedService<HubReference>();
        services.AddSignalR().AddJsonProtocol();

    }

    // Add services to the container.
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    // Add services to the container.

    // builder.Services.AddSwaggerGen(c =>
    // {
    //     c.SwaggerDoc("v1", new OpenApiInfo { Title = "Example API", Version = "v1" });

    //     c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    //     {
    //         Type = SecuritySchemeType.Http,
    //         BearerFormat = "JWT",
    //         In = ParameterLocation.Header,
    //         Scheme = "bearer",
    //         Description = "Please insert JWT token into field"
    //     });

    //     c.AddSecurityRequirement(new OpenApiSecurityRequirement
    // {
    //     {
    //         new OpenApiSecurityScheme
    //         {
    //             Reference = new OpenApiReference
    //             {
    //                 Type = ReferenceType.SecurityScheme,
    //                 Id = "Bearer"
    //             }
    //         },
    //         new string[] { }
    //     }
    // });
    // });

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthentication();
        app.UseRouting();
        app.UseAuthorization();
        //         app.MapControllers();

        var summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        app.UseEndpoints(e =>
        {
            e.MapControllers();
            e.MapHub<GameHub>("/gameHub");
        });
    }

    record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
    {
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }
}