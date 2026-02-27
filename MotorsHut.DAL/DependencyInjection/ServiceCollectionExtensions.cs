using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MotorsHut.DAL.Abstractions;
using MotorsHut.DAL.Abstractions.Repositories;
using MotorsHut.DAL.Data;
using MotorsHut.DAL.Entities;
using MotorsHut.DAL.Repositories;

namespace MotorsHut.DAL.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDal(this IServiceCollection services, IConfiguration configuration)
    {
        var rawConnectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");
        var connectionString = NormalizeConnectionString(rawConnectionString);
        var mySqlVersion = configuration["Database:MySqlVersion"] ?? "8.0.36";
        var parsed = Version.TryParse(mySqlVersion, out var version);
        var serverVersion = new MySqlServerVersion(parsed ? version! : new Version(8, 0, 36));

        services.AddDbContext<MotorsHutDbContext>(options =>
            options.UseMySql(
                connectionString,
                serverVersion,
                mySqlOptions =>
                {
                    mySqlOptions.MigrationsAssembly(typeof(MotorsHutDbContext).Assembly.FullName);
                    mySqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                }));

        services
            .AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
            })
            .AddEntityFrameworkStores<MotorsHutDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<ICarRepository, CarRepository>();
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    private static string NormalizeConnectionString(string connectionString)
    {
        // Keep app bootable when placeholder appsettings values are still present.
        return connectionString
            .Replace("YOUR_AIVEN_HOST", "localhost", StringComparison.OrdinalIgnoreCase)
            .Replace("YOUR_AIVEN_PORT", "3306", StringComparison.OrdinalIgnoreCase)
            .Replace("YOUR_AIVEN_USER", "root", StringComparison.OrdinalIgnoreCase)
            .Replace("YOUR_AIVEN_PASSWORD", string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
