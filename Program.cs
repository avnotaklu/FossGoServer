using System.Text;
using BadukServer;
using BadukServer.Models;
using BadukServer.Services;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var config = builder.Configuration;
Console.WriteLine("Startup: ");

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();
services.AddControllers();
services.AddSingleton<AuthenticationService>();
services.AddSingleton<UsersService>();
services.AddEndpointsApiExplorer();

builder.Services.Configure<DatabaseSettings>(
    builder.Configuration.GetSection("UserDatabase"));

builder.Services.Configure<MongodbCollectionParams<User>>(
    builder.Configuration.GetSection("UserCollection"));

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

// Add services to the container.
// builder.Services.AddAuthentication(x =>
// {
//     x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//     x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
//     x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
// }).AddJwtBearer(x =>
//     x.TokenValidationParameters = new TokenValidationParameters
//     {
//         // ValidIssuer = config["JwtSettings:Issuer"],
//         // ValidAudience = config["JwtSettings:Audience"],
//         IssuerSigningKey = new SymmetricSecurityKey
// (Encoding.UTF8.GetBytes(config["JwtSettings:Key"]!)),
//         ValidateIssuer = false,
//         ValidateAudience
// = false,
//         ValidateLifetime = true,
//         ValidateIssuerSigningKey = true
//     });

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.MapControllers();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    Console.WriteLine("hello");
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
