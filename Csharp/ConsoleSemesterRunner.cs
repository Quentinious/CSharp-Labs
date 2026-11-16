namespace Csharp;

internal static class ConsoleSemesterRunner
{
    public static void Run()
    {
        IRandomSource rand = new SystemRandomSource();
        var teacherFactory = new TeacherFactory(rand);
        var teachers = teacherFactory.CreateTeachers();
        ISkipStrategy strategy = new StudentStrategy();
        var simulator = new SemesterSimulator(teachers, strategy, rand);
        var result = simulator.Run();

        var averagePleasure = (double)result.Pleasure / SemesterSimulator.SemesterDays;
        Console.WriteLine($"Общее удовольствие за семестр: {result.Pleasure}");
        Console.WriteLine($"Среднее удовольствие в день: {averagePleasure}");
    }
}
