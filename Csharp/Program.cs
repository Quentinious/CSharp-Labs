using System.Diagnostics;
using Csharp;

internal class Program
{
    private const int SimulationCount = 1000;

    private static void Main()
    {
        var rand = new Random();
        var totalPleasure = 0L;
        var expelledSemesters = 0;
        var strategyName = new StudentStrategy().Name;
        var stopwatch = Stopwatch.StartNew();

        for (var iteration = 0; iteration < SimulationCount; iteration++)
        {
            var teachers = TeacherFactory.CreateTeachers(rand);
            ISkipStrategy strategy = new StudentStrategy();
            var simulator = new SemesterSimulator(teachers, strategy, rand);
            var result = simulator.Run();

            totalPleasure += result.Pleasure;
            if (result.Expelled)
            {
                expelledSemesters++;
            }
        }

        stopwatch.Stop();

        var averagePleasure = (double)totalPleasure / SimulationCount;
        var averagePleasurePerDay = averagePleasure / SemesterSimulator.SemesterDays;
        var expelledPercentage = (double)expelledSemesters / SimulationCount;

        Console.WriteLine($"Стратегия: {strategyName}");
        Console.WriteLine($"Количество семестров: {SimulationCount}");
        Console.WriteLine($"Среднее удовольствие за семестр: {averagePleasure:F2}");
        Console.WriteLine($"Среднее удовольствие в день: {averagePleasurePerDay:F2}");
        Console.WriteLine($"Вылетов: {expelledSemesters} ({expelledPercentage:P2})");
        Console.WriteLine($"Общее время: {stopwatch.ElapsedMilliseconds} мс");
    }
}