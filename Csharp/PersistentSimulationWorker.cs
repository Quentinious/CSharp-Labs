using Microsoft.Extensions.Hosting;

namespace Csharp;

internal sealed class PersistentSimulationWorker : BackgroundService
{
    private readonly PersistentSimulationService _simulation;
    private readonly IHostApplicationLifetime _lifetime;

    public PersistentSimulationWorker(PersistentSimulationService simulation, IHostApplicationLifetime lifetime)
    {
        _simulation = simulation;
        _lifetime = lifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested && _simulation.CanResume)
            {
                var result = await _simulation.SimulateAndSaveDayAsync(stoppingToken);
                Console.WriteLine($"День: {result.Day}; удовольствие: {result.TotalPleasure}; " +
                    $"среднее: {result.AveragePleasure:F2}; вылет: {result.Expelled}");
                if (!_simulation.CanResume)
                {
                    Console.WriteLine(result.Expelled ? "Студент вылетел. Семестр закрыт." : "100 дней завершены.");
                    _lifetime.StopApplication();
                    return;
                }
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The last committed day stays resumable; an interrupted transaction rolls back.
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Не удалось сохранить день ({ex.GetType().Name}). " +
                "Симуляция остановлена. Продолжите с последнего сохранения в БД.");
            Environment.ExitCode = 1;
            _lifetime.StopApplication();
        }
    }
}
