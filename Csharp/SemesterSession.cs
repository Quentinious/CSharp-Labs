using Csharp.Persistence;

namespace Csharp;

// Owns one semester in memory. No database access or SQL here.
internal sealed class SemesterSession
{
    private readonly SemesterState _initial;
    private readonly StatefulRandomSource _random;
    private readonly DaySimulationEngine _engine;
    private readonly List<SavedDay> _days;

    public bool CanResume => !_engine.IsExpelled && _engine.NextDay < SemesterSimulator.SemesterDays;

    public SemesterSession(SemesterState state)
    {
        _initial = state;
        _random = new StatefulRandomSource(state.RandomState);
        _days = new List<SavedDay>(state.Days);
        var strategy = new StudentStrategy(state.ObservationDays, state.RiskThreshold);
        // On DecideDay(nextDay), a fresh strategy replays days 0..nextDay-1
        // in exactly the same order as the continuous run (including visibility).
        _engine = new DaySimulationEngine(state.Teachers, strategy, _random,
            state.Days.Select(day => day.Lessons).ToArray(), state.TotalPleasure, state.Expelled);
    }

    public static SemesterSession CreateNew()
    {
        // Teacher generation cannot consume the question generator's sequence.
        var teachers = new TeacherFactory(StatefulRandomSource.Create()).CreateTeachers();
        var state = new SemesterState
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            Teachers = teachers,
            RandomState = StatefulRandomSource.Create().State
        };
        return new SemesterSession(state);
    }

    public DaySimulationResult SimulateDay()
    {
        if (!CanResume)
        {
            throw new InvalidOperationException("Семестр завершён или студент вылетел.");
        }

        var result = _engine.SimulateDay();
        _days.Add(new SavedDay(_engine.Days[^1], result.TotalPleasure, result.Expelled));
        return result;
    }

    public SemesterState CaptureState()
    {
        return new SemesterState
        {
            Id = _initial.Id,
            CreatedAt = _initial.CreatedAt,
            Teachers = _initial.Teachers,
            ObservationDays = _initial.ObservationDays,
            RiskThreshold = _initial.RiskThreshold,
            NextDay = _engine.NextDay,
            TotalPleasure = _engine.TotalPleasure,
            Expelled = _engine.IsExpelled,
            RandomState = _random.State,
            Days = new List<SavedDay>(_days)
        };
    }
}
