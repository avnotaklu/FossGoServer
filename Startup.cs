
using System.Text;
using BadukServer;
using BadukServer.Hubs;
using BadukServer.Models;
using BadukServer.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;


public class Startup
{
    private string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
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
        services.AddLogging(logging =>
            logging.AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                    options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss]";
                })
        );

        services.AddAuthorization(options =>
            options.AddPolicy("PlayerOnly", policy =>
                policy.RequireClaim("role", ["player"])
        ));

        services.AddCors(options =>
        {
            options.AddPolicy(MyAllowSpecificOrigins,
                            policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        });

        services.AddSingleton<AuthenticationService>();
        services.AddSingleton<IUsersService, UsersService>();
        services.AddSingleton<IUserRatingService, UserRatingService>();
        services.AddSingleton<IUserStatService, UserStatService>();
        services.AddSingleton<IGameService, GameService>();
        services.AddSingleton<IPlayerInfoService, PublicUserInfoService>();
        services.AddSingleton<IRatingEngine, RatingEngine>();
        services.AddSingleton<IStatCalculator, StatCalculator>();
        services.AddSingleton<IDateTimeService, DateTimeService>();
        services.AddSingleton<ITimeCalculator, TimeCalculator>();
        services.AddSingleton<ISignalRHubService, SignalRHubService>();
        services.AddSingleton<IMongoOperationLogger, MongoOperationHandler>();
        services.AddSingleton<MongodbService>();

        services.AddEndpointsApiExplorer();

        services.Configure<DatabaseSettings>(
            Configuration.GetSection("UserDatabase"));

        services.Configure<MongodbCollectionParams<User>>(
            Configuration.GetSection("UserCollection"));

        services.Configure<MongodbCollectionParams<PlayerRatings>>(
            Configuration.GetSection("UserRatingsCollection"));

        services.Configure<MongodbCollectionParams<UserStat>>(
            Configuration.GetSection("UserStatsCollection"));

        services.Configure<MongodbCollectionParams<Game>>(
            Configuration.GetSection("GameCollection"));

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
                IssuerSigningKey = new SymmetricSecurityKey
        (Encoding.UTF8.GetBytes(Configuration["JwtSettings:Key"]!)),
                ValidateIssuer = false,
                ValidateAudience
        = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true
            });

        services.AddSignalR().AddJsonProtocol().AddHubOptions<MainHub>(options =>
        {
            options.EnableDetailedErrors = true;
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        app.UseStaticFiles(new StaticFileOptions()
        {
            OnPrepareResponse = ctx =>
            {
                ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
                ctx.Context.Response.Headers.Append("Access-Control-Allow-Headers",
                  "Origin, X-Requested-With, Content-Type, Accept");
            },
        });

        app.UseCors(MyAllowSpecificOrigins);
        app.UseAuthentication();
        app.UseRouting();
        app.UseAuthorization();


        app.UseEndpoints(e =>
        {
            e.MapGet("/", () => "KeepAlive");
            e.MapControllers();
            e.MapHub<MainHub>("/mainHub");
        });

    }




}
