using System.Data;
using Npgsql;

namespace Csharp.Persistence;

internal sealed class SemesterRepository : ISemesterRepository
{
    private readonly string _connectionString;

    public SemesterRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        using var stream = typeof(SemesterRepository).Assembly
            .GetManifestResourceStream("Csharp.Persistence.schema.sql")!;
        using var reader = new StreamReader(stream);
        var sql = await reader.ReadToEndAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await ExecuteAsync(connection, transaction, sql, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task CreateAsync(SemesterState state, CancellationToken cancellationToken = default)
    {
        if (state.NextDay != 0 || state.Days.Count != 0 || state.Expelled || state.TotalPleasure != 0)
        {
            throw new ArgumentException("Новая сессия должна пустой быть");
        }
        if (state.Teachers.Length != 6 || state.Teachers.Select(t => t.Subject).Distinct().Count() != 6)
        {
            throw new ArgumentException("Нужны преподаватели для всех шести предметов.");
        }

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await ExecuteAsync(connection, transaction, """
            INSERT INTO semesters
                (id, created_at, status, next_day, total_pleasure, expelled,
                 random_state, observation_days, risk_threshold)
            VALUES ($1, $2, 'active', 0, 0, false, $3, $4, $5)
            """, cancellationToken, state.Id, state.CreatedAt, state.RandomState,
            state.ObservationDays, state.RiskThreshold);

        for (var position = 0; position < state.Teachers.Length; position++)
        {
            var teacher = state.Teachers[position];
            await ExecuteAsync(connection, transaction, """
                INSERT INTO semester_teachers (semester_id, subject, position, rule, depend_a, depend_b)
                VALUES ($1, $2, $3, $4, $5, $6)
                """, cancellationToken, state.Id, (int)teacher.Subject, position,
                (int)teacher.Rule, (int)teacher.DependA, (int)teacher.DependB);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task SaveDayAsync(Guid id, SavedDay day, long randomState,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
       
        await using (var command = Command(connection, transaction,
            "SELECT next_day, status FROM semesters WHERE id = $1 FOR UPDATE", id))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException($"Семестр {id} не найден.");
            }
        }

        if (day.Lessons.LessonCount < 1 || day.Lessons.LessonCount > 6 ||
            (!day.Expelled && day.Lessons.LessonCount != 6))
        {
            throw new ArgumentException("день должен содержать шесть занятий");
        }

        await ExecuteAsync(connection, transaction, """
            INSERT INTO simulation_days (semester_id, day, total_pleasure, expelled)
            VALUES ($1, $2, $3, $4)
            """, cancellationToken, id, day.Lessons.Day, day.TotalPleasure, day.Expelled);

        foreach (var (subject, lesson) in day.Lessons.Lessons)
        {
            await ExecuteAsync(connection, transaction, """
                INSERT INTO lesson_results (semester_id, day, subject, attended, was_actually_asked)
                VALUES ($1, $2, $3, $4, $5)
                """, cancellationToken, id, day.Lessons.Day, (int)subject,
                lesson.Attended, lesson.WasActuallyAsked);
        }

        var nextDay = day.Lessons.Day + 1;
        var status = day.Expelled ? "expelled" : nextDay == 100 ? "completed" : "active";
        await ExecuteAsync(connection, transaction, """
            UPDATE semesters SET next_day = $2, total_pleasure = $3, expelled = $4,
                random_state = $5, status = $6 WHERE id = $1
            """, cancellationToken, id, nextDay, day.TotalPleasure, day.Expelled, randomState, status);
        
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<SemesterState?> LoadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        // All four SELECTs see the same snapshot even if another process saves a day.
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        SemesterState state;
        await using (var command = Command(connection, transaction, """
            SELECT created_at, next_day, total_pleasure, expelled, random_state,
                observation_days, risk_threshold, random_version FROM semesters WHERE id = $1
            """, id))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }
            state = new SemesterState
            {
                Id = id, CreatedAt = reader.GetDateTime(0), NextDay = reader.GetInt32(1),
                TotalPleasure = reader.GetInt32(2), Expelled = reader.GetBoolean(3),
                RandomState = reader.GetInt64(4), ObservationDays = reader.GetInt32(5),
                RiskThreshold = reader.GetDouble(6), Teachers = new Teacher[6]
            };
        }

        var count = 0;
        await using (var command = Command(connection, transaction, """
            SELECT subject, rule, depend_a, depend_b FROM semester_teachers
            WHERE semester_id = $1 ORDER BY position
            """, id))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                state.Teachers[count++] = new Teacher((Subject)reader.GetInt32(0),
                    (TeacherRule)reader.GetInt32(1), (Subject)reader.GetInt32(2),
                    (Subject)reader.GetInt32(3));
            }
        }
        if (count != 6)
        {
            throw new InvalidOperationException("нужны шесть преподавателей.");
        }

        await using (var command = Command(connection, transaction, """
            SELECT day, total_pleasure, expelled FROM simulation_days WHERE semester_id = $1 ORDER BY day
            """, id))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var day = reader.GetInt32(0);
                if (day != state.Days.Count)
                {
                    throw new InvalidOperationException("пропущен день истории.");
                }
                state.Days.Add(new SavedDay(new DayResult(day), reader.GetInt32(1), reader.GetBoolean(2)));
            }
        }

        await using (var command = Command(connection, transaction, """
            SELECT day, subject, attended, was_actually_asked FROM lesson_results
            WHERE semester_id = $1 ORDER BY day, subject
            """, id))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                state.Days[reader.GetInt32(0)].Lessons.Add((Subject)reader.GetInt32(1),
                    new LessonResult(reader.GetBoolean(2), reader.GetBoolean(3)));
            }
        }

        if (state.Days.Count != state.NextDay || state.Days.Any(day =>
                day.Lessons.LessonCount == 0 || (!day.Expelled && day.Lessons.LessonCount != 6)) ||
            (state.Days.Count > 0))
        {
            throw new InvalidOperationException("прогресс и история не совпадают.");
        }

        await transaction.CommitAsync(cancellationToken);
        return state;
    }

    public async Task<IReadOnlyList<Guid>> ListActiveAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT id FROM semesters WHERE status = 'active' ORDER BY created_at, id", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var ids = new List<Guid>();
        while (await reader.ReadAsync(cancellationToken))
        {
            ids.Add(reader.GetGuid(0));
        }
        return ids;
    }

    private static NpgsqlCommand Command(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string sql, params object[] values)
    {
        var command = new NpgsqlCommand(sql, connection, transaction);
        foreach (var value in values)
        {
            command.Parameters.AddWithValue(value);
        }
        return command;
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string sql, CancellationToken cancellationToken, params object[] values)
    {
        await using var command = Command(connection, transaction, sql, values);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
