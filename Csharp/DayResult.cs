namespace Csharp;

public class DayResult
{
    private readonly Dictionary<Subject, LessonResult> _lessons = new();

    public int Day { get; }
    public int LessonCount => _lessons.Count;
    public IReadOnlyDictionary<Subject, LessonResult> Lessons => _lessons;

    public DayResult(int day)
    {
        Day = day;
    }

    public void Add(Subject subject, LessonResult result)
    {
        _lessons.Add(subject, result);
    }

    public LessonResult GetLesson(Subject subject)
    {
        return _lessons[subject];
    }
}
