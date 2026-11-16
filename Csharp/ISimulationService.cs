namespace Csharp;

public interface ISimulationService
{
    DaySimulationResult SimulateDay();

    bool IsExpelled { get; }
}
