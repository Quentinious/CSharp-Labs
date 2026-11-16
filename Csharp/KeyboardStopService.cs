using Microsoft.Extensions.Hosting;

namespace Csharp;

internal sealed class KeyboardStopService : BackgroundService
{
    private readonly IHostApplicationLifetime _applicationLifetime;

    public KeyboardStopService(IHostApplicationLifetime applicationLifetime)
    {
        _applicationLifetime = applicationLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (Console.IsInputRedirected)
        {
            return;
        }
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (Console.KeyAvailable)
                {
                    Console.ReadKey(intercept: true);
                    Console.WriteLine("Остановка по клавише");
                    _applicationLifetime.StopApplication();
                    return;
                }

                await Task.Delay(100, stoppingToken);
            }
        }
        catch (InvalidOperationException)
        {
           
        }
        
    }
}
