using Microsoft.AspNetCore.Identity;
using MotorsHut.DAL.Entities;

namespace MotorsHut.Extensions;

public static class AppInitializationExtensions
{
    private const string SuperAdminRoleName = "SuperAdmin";
    private static readonly string[] RequiredRoles = [SuperAdminRoleName, "Admin", "Customer"];

    public static async Task SeedRequiredRolesAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("RoleSeeder");

        try
        {
            await EnsureRolesAsync(roleManager, logger);
            await EnsureSuperAdminAsync(userManager, configuration, logger);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Role/user seeding skipped because the database is not reachable.");
        }
    }

    private static async Task EnsureRolesAsync(RoleManager<ApplicationRole> roleManager, ILogger logger)
    {
        foreach (var roleName in RequiredRoles)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var createResult = await roleManager.CreateAsync(new ApplicationRole { Name = roleName });
            if (!createResult.Succeeded)
            {
                logger.LogWarning("Role creation failed for {Role}. Errors: {Errors}", roleName, string.Join("; ", createResult.Errors.Select(e => e.Description)));
            }
        }
    }

    private static async Task EnsureSuperAdminAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger logger)
    {
        var email = configuration["Seed:SuperAdmin:Email"];
        var password = configuration["Seed:SuperAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation("SuperAdmin seed skipped: Seed:SuperAdmin:Email/Password is not configured.");
            return;
        }

        var userName = configuration["Seed:SuperAdmin:UserName"];
        var firstName = configuration["Seed:SuperAdmin:FirstName"] ?? "Super";
        var lastName = configuration["Seed:SuperAdmin:LastName"] ?? "Admin";

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                FirstName = firstName,
                LastName = lastName,
                UserName = string.IsNullOrWhiteSpace(userName) ? email : userName,
                Email = email,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                logger.LogWarning("SuperAdmin user creation failed. Errors: {Errors}", string.Join("; ", createResult.Errors.Select(e => e.Description)));
                return;
            }
        }

        if (!user.IsActive)
        {
            user.IsActive = true;
            await userManager.UpdateAsync(user);
        }

        if (!await userManager.IsInRoleAsync(user, SuperAdminRoleName))
        {
            var roleResult = await userManager.AddToRoleAsync(user, SuperAdminRoleName);
            if (!roleResult.Succeeded)
            {
                logger.LogWarning("Failed to assign SuperAdmin role. Errors: {Errors}", string.Join("; ", roleResult.Errors.Select(e => e.Description)));
            }
        }
    }
}
