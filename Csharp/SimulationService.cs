namespace Csharp;

internal  class SimulationService : ISimulationService
{
    private readonly DaySimulationEngine _engine;

    public SimulationService(ITeacherFactory teacherFactory, ISkipStrategy strategy, IRandomSource rand)
    {
        var teachers = teacherFactory.CreateTeachers();
        _engine = new DaySimulationEngine(teachers, strategy, rand);
    }

    public bool IsExpelled => _engine.IsExpelled;

    public DaySimulationResult SimulateDay()
    {
        return _engine.SimulateDay();
    }
}
