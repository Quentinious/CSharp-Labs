namespace Csharp;

public sealed class StudentStrategy : ISkipStrategy
{
    public string Name => "Консервативная стратегия";

    public bool[] DecideDay(int day, IReadOnlyStudentHistory history)
    {
        var subjectCount = Enum.GetValues<Subject>().Length;
        var attendance = new bool[subjectCount];

        for (var i = 0; i < attendance.Length; i++)
        {
            attendance[i] = true;
        }

        return attendance;
    }
}