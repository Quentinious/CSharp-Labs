namespace Csharp;

public interface ISkipStrategy
{
    string Name { get; }

    // Решение сразу на весь день: true — присутствовать, false — прогулять.
    bool[] DecideDay(int day, IReadOnlyStudentHistory history);
}

public interface IReadOnlyStudentHistory
{
    bool Attended(int day, Subject subject);

    // Для пропущенной пары результат опроса студенту неизвестен.
    bool? WasAsked(int day, Subject subject);
}
