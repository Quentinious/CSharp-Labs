using Csharp.Persistence;
using Npgsql;

namespace Csharp.Tests;

[Trait("Category", "Integration")]
public class SemesterPersistenceTests : IClassFixture<PostgresTestDatabase>
{
    private readonly PostgresTestDatabase _database;

    public SemesterPersistenceTests(PostgresTestDatabase database)
    {
        _database = database;
    }

    [PostgresFact]
    public async Task SaveAndLoad_RestoresState()
    {
        var session = new SemesterSession(KnownState());
        await _database.Repository.CreateAsync(session.CaptureState());
        await RunAndSave(session, 14);
        var expected = session.CaptureState();

        // A new repository/connection reads the data, not an in-memory cache.
        var repository = new SemesterRepository(_database.ConnectionString);
        var loaded = await repository.LoadAsync(expected.Id);

        Assert.NotNull(loaded);
        AssertStatesEqual(expected, loaded);
        AssertStatesEqual(loaded, new SemesterSession(loaded).CaptureState());
        Assert.Contains(loaded.Days.SelectMany(d => d.Lessons.Lessons.Values), l => !l.Attended);
    }

    [PostgresTheory]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(14)]
    public async Task Resume_MatchesContinuousRun(int savedDays)
    {
        var initial = KnownState();
        var continuous = new SemesterSession(initial);
        var expectedResults = new List<DaySimulationResult>();
        for (var day = 0; day < 40 && continuous.CanResume; day++)
        {
            expectedResults.Add(continuous.SimulateDay());
        }
        Assert.Equal(40, expectedResults.Count);

        var id = initial.Id;
        // Nothing from the old session escapes this method except the DB's UUID.
        await SavePrefix(initial, savedDays);
        var repository = new SemesterRepository(_database.ConnectionString);
        var loaded = await repository.LoadAsync(id);
        Assert.NotNull(loaded);
        Assert.Equal(savedDays, loaded.NextDay);
        var resumed = new SemesterSession(loaded); // new engine, strategy, history and RNG

        for (var day = savedDays; day < 40; day++)
        {
            var actual = resumed.SimulateDay();
            Assert.Equal(expectedResults[day], actual);
            var state = resumed.CaptureState();
            await repository.SaveDayAsync(id, state.Days[^1], state.RandomState);
        }

        var expected = continuous.CaptureState();
        var actualState = await repository.LoadAsync(id);
        Assert.NotNull(actualState);
        AssertStatesEqual(expected, actualState); // attendance AND questions, per lesson
        var randomQuestions = actualState.Days.Skip(savedDays)
            .Select(d => d.Lessons.GetLesson(Subject.Calculus).WasActuallyAsked).ToArray();
        Assert.Contains(true, randomQuestions);
        Assert.Contains(false, randomQuestions);
        Assert.Contains(actualState.Days.SelectMany(d => d.Lessons.Lessons.Values), l => !l.Attended);
        // Already persisted days cannot be credited or inserted a second time.
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.SaveDayAsync(id, actualState.Days[0], actualState.RandomState));
        var reloaded = await repository.LoadAsync(id);
        AssertStatesEqual(actualState, reloaded!);
    }

    [PostgresTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompletedSemester_CannotResume(bool expelled)
    {
        var state = KnownState(expelled ? 0 : 10, expelled ? 1.0 : 0.001, allDependent: !expelled);
        var session = new SemesterSession(state);
        await _database.Repository.CreateAsync(state);
        await RunAndSave(session, 100);

        var loaded = await _database.Repository.LoadAsync(state.Id);
        Assert.NotNull(loaded);
        Assert.False(loaded.CanResume);
        Assert.Equal(expelled, loaded.Expelled);
        var restored = new SemesterSession(loaded);
        Assert.Throws<InvalidOperationException>(() => restored.SimulateDay());
        var active = await _database.Repository.ListActiveAsync();
        Assert.DoesNotContain(state.Id, active);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _database.Repository.SaveDayAsync(
            state.Id, loaded.Days[^1], loaded.RandomState));

        if (expelled)
        {
            Assert.Equal(0, loaded.TotalPleasure);
            Assert.InRange(loaded.Days[^1].Lessons.LessonCount, 1, 5);
            Assert.Contains(loaded.Days[^1].Lessons.Lessons.Values, l => !l.Attended && l.WasActuallyAsked);
        }
        else
        {
            Assert.Equal(100, loaded.NextDay);
            Assert.Equal(99, loaded.Days[^1].Lessons.Day);
            Assert.Equal(640, loaded.TotalPleasure);
        }
    }

    [PostgresFact]
    public async Task Sessions_AreIndependent()
    {
        var first = new SemesterSession(KnownState());
        var second = new SemesterSession(KnownState(allDependent: true));
        await _database.Repository.CreateAsync(first.CaptureState());
        await _database.Repository.CreateAsync(second.CaptureState());
        await RunAndSave(first, 12);
        await RunAndSave(second, 4);

        var a = await _database.Repository.LoadAsync(first.CaptureState().Id);
        var b = await _database.Repository.LoadAsync(second.CaptureState().Id);
        AssertStatesEqual(first.CaptureState(), a!);
        AssertStatesEqual(second.CaptureState(), b!);
        Assert.NotEqual(a!.Id, b!.Id);
        Assert.NotEqual(a.Teachers[0].Rule, b.Teachers[0].Rule);
        Assert.Equal(12, a.Days.Count);
        Assert.Equal(4, b.Days.Count);
        var active = await _database.Repository.ListActiveAsync();
        Assert.Contains(a.Id, active);
        Assert.Contains(b.Id, active);
    }

    [PostgresFact]
    public async Task SaveFailure_RollsBack()
    {
        var state = KnownState();
        await _database.Repository.CreateAsync(state);
        var lessons = new DayResult(0);
        for (var subject = 0; subject < 5; subject++)
        {
            lessons.Add((Subject)subject, new LessonResult(true, false));
        }
        // The sixth INSERT violates a real FK after the day and five lessons were inserted.
        lessons.Add((Subject)99, new LessonResult(true, false));
        await Assert.ThrowsAsync<PostgresException>(() => _database.Repository.SaveDayAsync(
            state.Id, new SavedDay(lessons, 1, false), 9876));

        var loaded = await _database.Repository.LoadAsync(state.Id);
        AssertStatesEqual(state, loaded!);
        await using var connection = new NpgsqlConnection(_database.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT count(*) FROM lesson_results WHERE semester_id = $1", connection);
        command.Parameters.AddWithValue(state.Id);
        Assert.Equal(0L, await command.ExecuteScalarAsync());
        // A valid retry of day 0 succeeds: no partial rows or updated progress remain.
        await RunAndSave(new SemesterSession(loaded!), 1);
        var retried = await _database.Repository.LoadAsync(state.Id);
        Assert.Equal(1, retried!.NextDay);
        Assert.Equal(6, retried.Days[0].Lessons.LessonCount);
    }

    [PostgresFact]
    public async Task MissingSemester_ReturnsNull()
    {
        var state = await _database.Repository.LoadAsync(Guid.NewGuid());
        Assert.Null(state);
    }

    private async Task SavePrefix(SemesterState initial, int count)
    {
        var session = new SemesterSession(initial);
        await _database.Repository.CreateAsync(initial);
        await RunAndSave(session, count);
    }

    private async Task RunAndSave(SemesterSession session, int count)
    {
        var service = new PersistentSimulationService(_database.Repository, session);
        for (var day = 0; day < count && service.CanResume; day++)
        {
            await service.SimulateAndSaveDayAsync(CancellationToken.None);
        }
    }

    private static SemesterState KnownState(int observationDays = 10, double risk = 0.001,
        bool allDependent = false)
    {
        var teachers = new Teacher[6];
        foreach (var subject in Enum.GetValues<Subject>())
        {
            teachers[(int)subject] = new Teacher(subject, TeacherRule.DependA, subject, Subject.Physics);
        }
        if (!allDependent)
        {
            teachers[0] = new Teacher(Subject.Calculus, TeacherRule.Check50, Subject.English, Subject.Physics);
            teachers[1] = new Teacher(Subject.LinearAlgebra, TeacherRule.Check50, Subject.DiscreteMath, Subject.English);
            teachers[2] = new Teacher(Subject.DiscreteMath, TeacherRule.DependA, Subject.Calculus, Subject.English);
            teachers[3] = new Teacher(Subject.Programming, TeacherRule.XorAandB, Subject.Calculus, Subject.LinearAlgebra);
        }
        return new SemesterState
        {
            Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, Teachers = teachers,
            RandomState = 4815162342, ObservationDays = observationDays, RiskThreshold = risk
        };
    }

    private static void AssertStatesEqual(SemesterState expected, SemesterState actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        // PostgreSQL timestamptz has microsecond precision, DateTime has 100 ns ticks.
        Assert.Equal(expected.CreatedAt.Ticks / 10, actual.CreatedAt.Ticks / 10);
        Assert.Equal(expected.NextDay, actual.NextDay);
        Assert.Equal(expected.TotalPleasure, actual.TotalPleasure);
        Assert.Equal(expected.Expelled, actual.Expelled);
        Assert.Equal(expected.Status, actual.Status);
        Assert.Equal(expected.RandomState, actual.RandomState);
        Assert.Equal(expected.ObservationDays, actual.ObservationDays);
        Assert.Equal(expected.RiskThreshold, actual.RiskThreshold);
        Assert.Equal(expected.Teachers.Length, actual.Teachers.Length);
        for (var i = 0; i < expected.Teachers.Length; i++)
        {
            Assert.Equal(expected.Teachers[i].Subject, actual.Teachers[i].Subject);
            Assert.Equal(expected.Teachers[i].Rule, actual.Teachers[i].Rule);
            Assert.Equal(expected.Teachers[i].DependA, actual.Teachers[i].DependA);
            Assert.Equal(expected.Teachers[i].DependB, actual.Teachers[i].DependB);
        }
        Assert.Equal(expected.Days.Count, actual.Days.Count);
        for (var day = 0; day < expected.Days.Count; day++)
        {
            var a = expected.Days[day];
            var b = actual.Days[day];
            Assert.Equal(a.TotalPleasure, b.TotalPleasure);
            Assert.Equal(a.Expelled, b.Expelled);
            Assert.Equal(a.Lessons.Day, b.Lessons.Day);
            Assert.Equal(a.Lessons.LessonCount, b.Lessons.LessonCount);
            foreach (var (subject, lesson) in a.Lessons.Lessons)
            {
                var restored = b.Lessons.GetLesson(subject);
                Assert.Equal(lesson.Attended, restored.Attended);
                Assert.Equal(lesson.WasActuallyAsked, restored.WasActuallyAsked);
            }
        }
    }
}
