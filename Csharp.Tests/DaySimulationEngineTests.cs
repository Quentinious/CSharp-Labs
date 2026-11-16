using Csharp.Tests.TestDoubles;

namespace Csharp.Tests;

public class DaySimulationEngineTests
{
    [Theory]
    [InlineData(true, false, 1, false)]
    [InlineData(true, true, 1, false)]
    [InlineData(false, false, 2, false)]
    [InlineData(false, true, 0, true)]
    public void AttendanceAndAsk_ReturnExpected(
        bool attended, bool asked, int expectedPleasure, bool expectedExpelled)
    {
        
        Teacher[] teachers = Check50Teachers();
        bool[] attendance = { true, true, true, true, true, true };
        attendance[(int)Subject.Calculus] = attended;
        var strategy = new StubSkipStrategy(attendance);

        int firstRandomValue = 1;
        if (asked)
        {
            firstRandomValue = 0;
        }
        var random = new SequenceRandomSource(firstRandomValue, 1, 1, 1, 1, 1);
        var engine = new DaySimulationEngine(teachers, strategy, random);

       
        var result = engine.SimulateDay();

    
        Assert.Equal(0, result.Day);
        Assert.Equal(expectedPleasure, result.TotalPleasure);
        Assert.Equal((double)expectedPleasure, result.AveragePleasure);
        Assert.Equal(expectedExpelled, result.Expelled);
        Assert.Equal(expectedExpelled, engine.IsExpelled);
    }

    [Fact]
    public void History_UpdatesAfterDay()
    {
       
        Teacher[] teachers = Check50Teachers();
        bool[] firstAttendance = { false, true, true, true, true, true };
        bool[] secondAttendance = { true, true, true, true, true, true };
        var strategy = new StubSkipStrategy(firstAttendance);
        var random = new SequenceRandomSource(1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1);
        var engine = new DaySimulationEngine(teachers, strategy, random);

       
        var first = engine.SimulateDay();
        strategy.Attendance = secondAttendance;
        var second = engine.SimulateDay();

       
        Assert.False(strategy.PreviousAttendance[(int)Subject.Calculus]);
        Assert.Null(strategy.PreviousQuestions[(int)Subject.Calculus]);
        Assert.True(strategy.PreviousAttendance[(int)Subject.LinearAlgebra]);
        Assert.True(strategy.PreviousQuestions[(int)Subject.LinearAlgebra]);
        Assert.False(strategy.PreviousQuestions[(int)Subject.Physics]);
        Assert.Equal(2, first.TotalPleasure);
        Assert.Equal(3, second.TotalPleasure);
        Assert.Equal(1.5, second.AveragePleasure);
        Assert.Equal(new[] { 0, 1 }, strategy.CalledDays);
    }

    [Fact]
    public void Expulsion_ResetsPleasure()
    {
       
        Teacher[] teachers = Check50Teachers();
        teachers[(int)Subject.LinearAlgebra] = new Teacher(
            Subject.LinearAlgebra, TeacherRule.DependA, Subject.Calculus, Subject.Physics);
        bool[] firstAttendance = { true, true, true, true, true, true };
        bool[] secondAttendance = { true, false, true, true, true, true };
        var strategy = new StubSkipStrategy(firstAttendance);
        var random = new SequenceRandomSource(0, 1, 1, 1, 1, 1, 1);
        var engine = new DaySimulationEngine(teachers, strategy, random);

        
        var first = engine.SimulateDay();
        strategy.Attendance = secondAttendance;
        var second = engine.SimulateDay();

       
        Assert.Equal(1, first.TotalPleasure);
        Assert.True(second.Expelled);
        Assert.Equal(0, second.TotalPleasure);
        Assert.Equal(0, second.AveragePleasure);
        Assert.Equal(new[] { 0, 1 }, strategy.CalledDays);
    }

    [Fact]
    public void AfterExpulsion_Throws()
    {
        
        Teacher[] teachers = Check50Teachers();
        bool[] attendance = { false, false, false, false, false, false };
        var strategy = new StubSkipStrategy(attendance);
        var random = new SequenceRandomSource(0, 1, 1, 1, 1, 1);
        var engine = new DaySimulationEngine(teachers, strategy, random);

      
        var result = engine.SimulateDay();

        
        Assert.True(result.Expelled);
        Assert.Throws<InvalidOperationException>(() => engine.SimulateDay());
        Assert.Equal(new[] { 0 }, strategy.CalledDays);
        Assert.Equal(5, random.Remaining);
    }
    
    private static Teacher[] Check50Teachers()
    {
        Subject[] subjects = Enum.GetValues<Subject>();
        var teachers = new Teacher[subjects.Length];
        for (int i = 0; i < subjects.Length; i++)
        {
            teachers[i] = new Teacher(
                subjects[i], TeacherRule.Check50, Subject.Calculus, Subject.LinearAlgebra);
        }
        return teachers;
    }
}
