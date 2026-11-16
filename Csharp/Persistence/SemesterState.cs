namespace Csharp.Persistence;

internal sealed class SemesterState
{
    public Guid Id { get; init; }
    public DateTime CreatedAt { get; init; }
    public int NextDay { get; init; }
    public int TotalPleasure { get; init; }
    public bool Expelled { get; init; }
    public long RandomState { get; init; }
    public int ObservationDays { get; init; } = 10;
    public double RiskThreshold { get; init; } = 0.001;
    public Teacher[] Teachers { get; init; } = Array.Empty<Teacher>();
    public List<SavedDay> Days { get; init; } = new();
    public bool CanResume => !Expelled && NextDay < SemesterSimulator.SemesterDays;
    public string Status => Expelled ? "expelled" : CanResume ? "active" : "completed";
}

internal sealed class SavedDay
{
    public DayResult Lessons { get; }
    public int TotalPleasure { get; }
    public bool Expelled { get; }

    public SavedDay(DayResult lessons, int totalPleasure, bool expelled)
    {
        Lessons = lessons;
        TotalPleasure = totalPleasure;
        Expelled = expelled;
    }
}
