namespace Csharp;

internal static class BenchmarkRunner
{
    private const int SimulationCount = 1000;

    public static void Run()
    {
        IRandomSource rand = new SystemRandomSource();
        var totalPleasure = 0.0;
        var teacherFactory = new TeacherFactory(rand);

        for (var iteration = 0; iteration < SimulationCount; iteration++)
        {
            var teachers = teacherFactory.CreateTeachers();
            ISkipStrategy strategy = new StudentStrategy();
            var simulator = new SemesterSimulator(teachers, strategy, rand);
            var result = simulator.Run();

            totalPleasure += result.Pleasure;
        }

        var averagePleasure = totalPleasure / SimulationCount;
        var averagePleasurePerDay = averagePleasure / SemesterSimulator.SemesterDays;

        Console.WriteLine($"Среднее удовольствие за семестр: {averagePleasure}");
        Console.WriteLine($"Среднее удовольствие в день: {averagePleasurePerDay}");
    }
}
