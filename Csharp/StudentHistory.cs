namespace Csharp;

internal sealed class StudentHistory : IReadOnlyStudentHistory
{
    private readonly List<DayResult> _days = new();

    public bool Attended(int day, Subject subject)
    {
        return GetDay(day).GetLesson(subject).Attended;
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

    internal bool WasActuallyAsked(int day, Subject subject)
    {
        return GetDay(day).GetLesson(subject).WasActuallyAsked;
    }

    internal void Add(DayResult dayResult)
    {
        var subjectCount = Enum.GetValues<Subject>().Length;
        if (dayResult.Day != _days.Count || dayResult.LessonCount != subjectCount)
        {
            throw new InvalidOperationException("В историю можно добавить только следующий полностью обработанный день.");
        }

        _days.Add(dayResult);
    }

    private DayResult GetDay(int day)
    {
        if (day < 0 || day >= _days.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(day), "В истории нет указанного дня.");
        }

        return _days[day];
    }
}
