namespace Csharp;

internal sealed class SemesterSimulator
{
    public const int SemesterDays = 100;

    private readonly DaySimulationEngine _engine;
    private SemesterResult? _result;

    public SemesterSimulator(Teacher[] teachers, ISkipStrategy strategy, IRandomSource rand)
    {
        _engine = new DaySimulationEngine(teachers, strategy, rand);
    }

    public SemesterResult Run()
    {
        if (_result is not null)
        {
            return _result;
        }

        var pleasure = 0;
        for (var day = 0; day < SemesterDays; day++)
        {
            var result = _engine.SimulateDay();
            if (result.Expelled)
            {
                _result = new SemesterResult(0, true);
                return _result;
            }

            pleasure = result.TotalPleasure;
        }

        _result = new SemesterResult(pleasure, false);
        return _result;
    }
}
