using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MotorsHut.BLL.Abstractions.Services;
using MotorsHut.BLL.Services;
using MotorsHut.DAL.DependencyInjection;

namespace MotorsHut.BLL.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBll(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDal(configuration);
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICarService, CarService>();
        return services;
    }
}
