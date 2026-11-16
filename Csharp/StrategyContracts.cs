namespace Csharp;

public interface ISkipStrategy
{
    string Name { get; }
    bool[] DecideDay(int day, IReadOnlyStudentHistory history);
}

public interface IReadOnlyStudentHistory
{
    bool Attended(int day, Subject subject);
    bool? WasAsked(int day, Subject subject);
}
