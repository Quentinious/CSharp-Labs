namespace Csharp;

internal sealed class StudentHistory : IReadOnlyStudentHistory
{
    private List<DayResult> _days = new();
    public IReadOnlyList<DayResult> Days => _days.AsReadOnly();

    public bool Attended(int day, Subject subject)
    {
        var dayResult = GetDay(day);
        var lesson = dayResult.GetLesson(subject);
        return lesson.Attended;
    }

    public bool? WasAsked(int day, Subject subject)
    {
        var lesson = GetDay(day).GetLesson(subject);
        if (!lesson.Attended)
        {
            return null;
        }

        return lesson.WasActuallyAsked;
    }

    public bool WasActuallyAsked(int day, Subject subject)
    {
        return GetDay(day).GetLesson(subject).WasActuallyAsked;
    }

    public void Add(DayResult dayResult)
    {
        _days.Add(dayResult);
    }

    private DayResult GetDay(int day)
    {
        return _days[day];
    }
}
