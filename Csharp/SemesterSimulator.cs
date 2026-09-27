namespace Csharp;

internal sealed class SemesterSimulator
{
    public const int SemesterDays = 100;

    private readonly Teacher[] _teachers;
    private readonly ISkipStrategy _strategy;
    private readonly Random _rand;

    public SemesterSimulator(Teacher[] teachers, ISkipStrategy strategy, Random rand)
    {
        _teachers = teachers;
        _strategy = strategy;
        _rand = rand;
    }

    public SemesterResult Run()
    {
        var history = new StudentHistory();
        var pleasure = 0;

        for (var day = 0; day < SemesterDays; day++)
        {
            var attendance = _strategy.DecideDay(day, history);
            ValidateAttendance(attendance);

            var dayResult = new DayResult(day);
            pleasure++;

            foreach (var teacher in _teachers)
            {
                var attended = attendance[(int)teacher.Subject];
                var asked = teacher.WillAsk(day, history, _rand);
                var lessonResult = new LessonResult(attended, asked);
                dayResult.Add(teacher.Subject, lessonResult);

                if (!attended)
                {
                    pleasure++;
                    if (asked)
                    {
                        return new SemesterResult(0, true);
                    }
                }
            }

            history.Add(dayResult);
        }

        return new SemesterResult(pleasure, false);
    }

    private static void ValidateAttendance(bool[]? attendance)
    {
        var subjectCount = Enum.GetValues<Subject>().Length;

        if (attendance is null)
        {
            throw new InvalidOperationException("Стратегия вернула null вместо решений на день.");
        }

        if (attendance.Length != subjectCount)
        {
            throw new InvalidOperationException(
                $"Стратегия должна вернуть {subjectCount} решений, но вернула {attendance.Length}.");
        }
    }
}
