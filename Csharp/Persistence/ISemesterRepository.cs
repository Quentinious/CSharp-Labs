namespace Csharp.Persistence;

internal interface ISemesterRepository
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task CreateAsync(SemesterState state, CancellationToken cancellationToken = default);
    Task SaveDayAsync(Guid id, SavedDay day, long randomState, CancellationToken cancellationToken = default);
    Task<SemesterState?> LoadAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> ListActiveAsync(CancellationToken cancellationToken = default);
}
