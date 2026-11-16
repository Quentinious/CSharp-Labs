using Microsoft.Extensions.DependencyInjection;

namespace Csharp;

public static class SimulationServices
{
    public static IServiceCollection AddSimulationServices(this IServiceCollection services)
    {
        services.AddSingleton<IRandomSource, SystemRandomSource>();
        services.AddSingleton<ITeacherFactory, TeacherFactory>();
        services.AddSingleton<ISkipStrategy, StudentStrategy>();
        services.AddSingleton<ISimulationService, SimulationService>();
        return services;
    }
}
