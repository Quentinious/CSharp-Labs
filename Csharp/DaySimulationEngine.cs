namespace Csharp;

internal sealed class DaySimulationEngine
{
    private readonly Teacher[] _teachers;
    private readonly ISkipStrategy _strategy;
    private readonly IRandomSource _rand;
    private readonly StudentHistory _history = new();

    private int _pleasure;
    private int _nextDay;
    private bool _expelled;

    public bool IsExpelled => _expelled;
    public int NextDay => _nextDay;
    public int TotalPleasure => _pleasure;
    public IReadOnlyList<DayResult> Days => _history.Days;

    public DaySimulationEngine(Teacher[] teachers, ISkipStrategy strategy, IRandomSource rand)
    {
        _teachers = teachers;
        _strategy = strategy;
        _rand = rand;
    }

    public DaySimulationEngine(Teacher[] teachers, ISkipStrategy strategy, IRandomSource rand,
        IReadOnlyList<DayResult> days, int totalPleasure, bool expelled)
        : this(teachers, strategy, rand)
    {
        foreach (var day in days)
        {
            _history.Add(day);
        }

        _nextDay = days.Count;
        _pleasure = totalPleasure;
        _expelled = expelled;
    }
    
    public DaySimulationResult SimulateDay()
    {
        if (_expelled)
        {
            throw new InvalidOperationException("студент уже вылетел.");
        }

        var day = _nextDay;
        var attendance = _strategy.DecideDay(day, _history);
        ValidateAttendance(attendance);
        
        var dayResult = new DayResult(day);
        _pleasure++;
        
        foreach (var teacher in _teachers)
        {
            var attended = attendance[(int)teacher.Subject];
            var asked = teacher.WillAsk(day, _history, _rand);
            var lessonResult = new LessonResult(attended, asked);
            dayResult.Add(teacher.Subject, lessonResult);

            if (!attended)
            {
                _pleasure++;
                if (asked)
                {
                    _pleasure = 0;
                    _expelled = true;
                    _history.Add(dayResult);
                    _nextDay++;
                    return new DaySimulationResult(day, 0, 0, true);
                }
            }
        }

        _history.Add(dayResult);
        _nextDay++;
        var averagePleasure = (double)_pleasure / _nextDay;
        return new DaySimulationResult(day, _pleasure, averagePleasure, false);
    }

    private static void ValidateAttendance(bool[]? attendance)
    {
        if (attendance is null || attendance.Length != Enum.GetValues<Subject>().Length)
        {
            throw new ArgumentException("Стратегия должна вернуть решение для всех шести предметов.", nameof(attendance));
        }
    }
}
