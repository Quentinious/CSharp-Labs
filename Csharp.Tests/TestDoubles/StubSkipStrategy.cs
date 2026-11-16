namespace Csharp.Tests.TestDoubles;

internal sealed class StubSkipStrategy : ISkipStrategy
{
    public bool[] Attendance { get; set; }
    public List<int> CalledDays { get; }
    public bool[] PreviousAttendance { get; }
    public bool?[] PreviousQuestions { get; }

    public StubSkipStrategy(bool[] attendance)
    {
        Attendance = attendance;
        CalledDays = new List<int>();
        PreviousAttendance = new bool[6];
        PreviousQuestions = new bool?[6];
    }

    public string Name
    {
        get
        {
            return "To be";
        }
    }

    public bool[] DecideDay(int day, IReadOnlyStudentHistory history)
    {
        CalledDays.Add(day);
        if (day > 0)
        {
            foreach (Subject subject in Enum.GetValues<Subject>())
            {
                int index = (int)subject;
                PreviousAttendance[index] = history.Attended(day - 1, subject);
                PreviousQuestions[index] = history.WasAsked(day - 1, subject);
            }
        }

        return Attendance;
    }
}
