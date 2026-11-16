using Csharp;
using Csharp.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

internal class Program
{
    private static async Task Main(string[] args)
    {
        if (args.Length > 0 && args[0] is "--new-semester" or "--resume-semester" or "--list-semesters")
        {
            try
            {
                await RunPersistentAsync(args);
            }
            catch (Exception ex)
            {
                // Connection strings/passwords must not appear in error output.
                Console.Error.WriteLine(ex is ArgumentException or InvalidOperationException
                    ? ex.Message : $"Ошибка PostgreSQL ({ex.GetType().Name}). Проверьте подключение и схему БД.");
                Environment.ExitCode = 1;
            }
            return;
        }

        if (args.Length > 0 && args[0] == "--semester")
        {
            ConsoleSemesterRunner.Run();
            return;
        }

        if (args.Length > 0 && args[0] == "--benchmark")
        {
            BenchmarkRunner.Run();
            return;
        }

        var builder = Host.CreateApplicationBuilder(args);
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.Services.AddSimulationServices();
        builder.Services.AddHostedService<SimulationWorker>();
        builder.Services.AddHostedService<KeyboardStopService>();

        var host = builder.Build();
        await host.RunAsync();
    }

    private static async Task RunPersistentAsync(string[] args)
    {
        var resume = args[0] == "--resume-semester";
        var id = Guid.Empty;
        if ((resume && (args.Length != 2 || !Guid.TryParse(args[1], out id))) ||
            (!resume && args.Length != 1))
        {
            throw new ArgumentException("Использование: --new-semester | --resume-semester <UUID> | --list-semesters");
        }

        // These are application modes, not configuration command-line key/value pairs.
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        var connectionString = builder.Configuration.GetConnectionString("Postgres");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Задайте строку подключения в ConnectionStrings__Postgres.");
        }

        ISemesterRepository repository = new SemesterRepository(connectionString);
        await repository.InitializeAsync();
        if (args[0] == "--list-semesters")
        {
            var ids = await repository.ListActiveAsync();
            Console.WriteLine(ids.Count == 0 ? "Незавершённых семестров нет." : "Незавершённые семестры:");
            foreach (var activeId in ids)
            {
                Console.WriteLine(activeId);
            }
            return;
        }

        SemesterSession session;
        if (resume)
        {
            var state = await repository.LoadAsync(id);
            if (state is null)
            {
                throw new InvalidOperationException($"Семестр {id} не найден.");
            }
            if (!state.CanResume)
            {
                throw new InvalidOperationException("Семестр завершён или студент вылетел. Продолжение невозможно.");
            }
            session = new SemesterSession(state);
        }
        else
        {
            session = SemesterSession.CreateNew();
            await repository.CreateAsync(session.CaptureState());
        }

        var current = session.CaptureState();
        Console.WriteLine($"Семестр: {current.Id}; следующий день: {current.NextDay}");
        Console.WriteLine("Любая клавиша или Ctrl+C — остановка. Дни нумеруются от 0 до 99.");
        builder.Services.AddSingleton(repository);
        builder.Services.AddSingleton(new PersistentSimulationService(repository, session));
        builder.Services.AddHostedService<PersistentSimulationWorker>();
        builder.Services.AddHostedService<KeyboardStopService>();
        using var host = builder.Build();
        await host.RunAsync();
    }
}
