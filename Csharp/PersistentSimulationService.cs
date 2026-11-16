using Csharp.Persistence;

namespace Csharp;

internal sealed class PersistentSimulationService
{
    private readonly ISemesterRepository _repository;
    private readonly SemesterSession _session;
    private bool _saveFailed;

    public PersistentSimulationService(ISemesterRepository repository, SemesterSession session)
    {
        _repository = repository;
        _session = session;
    }

    public bool CanResume => !_saveFailed && _session.CanResume;

    public async Task<DaySimulationResult> SimulateAndSaveDayAsync(CancellationToken cancellationToken)
    {
        if (_saveFailed)
        {
            throw new InvalidOperationException("После ошибки сохранения загрузите семестр из БД заново.");
        }
        cancellationToken.ThrowIfCancellationRequested();
        var result = _session.SimulateDay();
        var state = _session.CaptureState();
        try
        {
            await _repository.SaveDayAsync(state.Id, state.Days[^1], state.RandomState, cancellationToken);
        }
        catch
        {
            // Never continue from unsaved in-memory progress, including an ambiguous commit.
            _saveFailed = true;
            throw;
        }
        return result;
    }
}
