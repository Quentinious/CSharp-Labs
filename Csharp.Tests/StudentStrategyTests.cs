namespace Csharp.Tests;

public class StudentStrategyTests
{
    [Fact]
    public void EmptyHistory_AttendsAll()
    {
        var history = new StudentHistory();
        var strategy = new StudentStrategy();

       
        bool[] attendance = strategy.DecideDay(0, history);

        Assert.Equal(6, attendance.Length);
        foreach (bool attended in attendance)
        {
            Assert.True(attended);
        }
    }

    [Fact]
    public void ObservationDays_AttendsAll()
    {
        var history = CreateHistory(9, asked: false);
        var strategy = new StudentStrategy();
        
        bool[] attendance = strategy.DecideDay(9, history);
        
        foreach (bool attended in attendance)
        {
            Assert.True(attended);
        }
    }

    [Fact]
    public void NeverAsked_SkipsAll()
    {
        var history = CreateHistory(10, asked: false);
        var strategy = new StudentStrategy();
        
        bool[] attendance = strategy.DecideDay(10, history);
        
        foreach (bool attended in attendance)
        {
            Assert.False(attended);
        }
    }

    [Fact]
    public void AlwaysAsked_AttendsAll()
    {
        var history = CreateHistory(10, asked: true);
        var strategy = new StudentStrategy();
        
        bool[] attendance = strategy.DecideDay(10, history);
        
        foreach (bool attended in attendance)
        {
            Assert.True(attended);
        }
    }

    [Fact]
    public void MixedHistory_AttendsAll()
    {
        var history = new StudentHistory();
        AddDay(history, 0, asked: true);
        for (int day = 1; day < 10; day++)
        {
            AddDay(history, day, asked: false);
        }
        var strategy = new StudentStrategy();
        
        bool[] attendance = strategy.DecideDay(10, history);

       
        foreach (bool attended in attendance)
        {
            Assert.True(attended);
        }
    }

    [Theory]
    [InlineData(Subject.Calculus)]
    [InlineData(Subject.LinearAlgebra)]
    [InlineData(Subject.DiscreteMath)]
    [InlineData(Subject.Programming)]
    [InlineData(Subject.Physics)]
    [InlineData(Subject.English)]
    public void Subjects_DecidedSeparately(Subject askedSubject)
    {
     
        var history = new StudentHistory();
        for (int day = 0; day < 10; day++)
        {
            var dayResult = new DayResult(day);
            foreach (Subject subject in Enum.GetValues<Subject>())
            {
                bool asked = day == 0 && subject == askedSubject;
                dayResult.Add(subject, new LessonResult(true, asked));
            }
            history.Add(dayResult);
        }
        var strategy = new StudentStrategy();

     
        bool[] attendance = strategy.DecideDay(10, history);

       
        foreach (Subject subject in Enum.GetValues<Subject>())
        {
            bool expected = subject == askedSubject;
            Assert.Equal(expected, attendance[(int)subject]);
        }
    }
    
    private static StudentHistory CreateHistory(int count, bool asked, bool attended = true)
    {
        var history = new StudentHistory();
        for (int day = 0; day < count; day++)
        {
            AddDay(history, day, asked, attended);
        }
        return history;
    }

    private static void AddDay(StudentHistory history, int day, bool asked, bool attended = true)
    {
        var dayResult = new DayResult(day);
        foreach (Subject subject in Enum.GetValues<Subject>())
        {
            dayResult.Add(subject, new LessonResult(attended, asked));
        }
        history.Add(dayResult);
    }
}
