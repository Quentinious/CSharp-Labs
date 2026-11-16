using Microsoft.Extensions.Hosting;

namespace Csharp;

public class SimulationWorker : BackgroundService
{
    private readonly ISimulationService _simulationService;
    private readonly IHostApplicationLifetime _applicationLifetime;

    public SimulationWorker(ISimulationService simulationService, IHostApplicationLifetime applicationLifetime)
    {
        _simulationService = simulationService;
        _applicationLifetime = applicationLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = _simulationService.SimulateDay();

                Console.WriteLine($"День: {result.Day}");
                Console.WriteLine($"Общее удовольствие: {result.TotalPleasure}");
                Console.WriteLine($"Среднее удовольствие в день: {result.AveragePleasure:F2}");
                Console.WriteLine();

                if (result.Expelled)
                {
                    Console.WriteLine("Вылетел");
                    _applicationLifetime.StopApplication();
                    return;
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
    }
}
