using Microsoft.AspNetCore.Authentication.JwtBearer; // Use 'Microsoft.AspNetCore.Authentication.JwtBearer' Nuget package.
using Microsoft.IdentityModel.Tokens;


namespace Client_Credentias_Flow;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 1. Add Authentication Services
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = builder.Configuration["Auth:Issuer"];
                options.Audience = builder.Configuration["Auth:Audience"];

                Console.WriteLine($"Authority: {options.Authority}");
                Console.WriteLine($"Audience: {options.Audience}");

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["Auth:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["Auth:Audience"],
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        Console.WriteLine($"Token validated for user: {context.Principal?.Identity?.Name}");
                        return Task.CompletedTask;
                    }
                };
            });

        builder.Services.AddAuthorization();

        var app = builder.Build();


        // 2. Enable Authentication Middleware
        // Order is critical: Authentication MUST come before Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        var summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        // 3. Secure the Endpoint
        app.MapGet("/weatherforecast", (HttpContext httpContext) =>
            {
                var forecast = Enumerable.Range(1, 5).Select(index =>
                        new WeatherForecast
                        {
                            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                            TemperatureC = Random.Shared.Next(-20, 55),
                            Summary = summaries[Random.Shared.Next(summaries.Length)]
                        })
                    .ToArray();
                return forecast;
            })
            .WithName<IEndpointConventionBuilder>("GetWeatherForecast")
            .RequireAuthorization(); // This line ensures the JWT is present and valid

        app.Run();
    }
}