using Csharp.Tests.TestDoubles;

namespace Csharp.Tests;

public class TeacherTests
{
    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void Check50_ReturnsExpected(int value, bool expected)
    {
        var teacher = new Teacher(
            Subject.Calculus, TeacherRule.Check50, Subject.LinearAlgebra, Subject.Physics);
        var history = new StudentHistory();
        var random = new SequenceRandomSource(value);
        
        bool result = teacher.WillAsk(0, history, random);
        
        Assert.Equal(expected, result);
        Assert.Equal(0, random.Remaining);
    }

    [Fact]
    public void Check50_UsesSequence()
    {
        var teacher = new Teacher(
            Subject.Calculus, TeacherRule.Check50, Subject.LinearAlgebra, Subject.Physics);
        var history = new StudentHistory();
        var random = new SequenceRandomSource(0, 1, 1, 0);
        
        bool first = teacher.WillAsk(0, history, random);
        bool second = teacher.WillAsk(0, history, random);
        bool third = teacher.WillAsk(0, history, random);
        bool fourth = teacher.WillAsk(0, history, random);
        
        Assert.True(first);
        Assert.False(second);
        Assert.False(third);
        Assert.True(fourth);
        Assert.Equal(0, random.Remaining);
    }

    [Fact]
    public void DependA_FirstDay()
    {
        var teacher = new Teacher(
            Subject.Calculus, TeacherRule.DependA, Subject.LinearAlgebra, Subject.Physics);
        var history = new StudentHistory();
        var random = new SequenceRandomSource();
        
        bool result = teacher.WillAsk(0, history, random);
        
        Assert.False(result);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DependA_FollowsSource(bool sourceAsked)
    {
        var teacher = new Teacher(
            Subject.Calculus, TeacherRule.DependA, Subject.LinearAlgebra, Subject.Physics);
        var yesterday = new DayResult(0);
        
        yesterday.Add(Subject.LinearAlgebra, new LessonResult(true, sourceAsked));
        var history = new StudentHistory();
        history.Add(yesterday);
        var random = new SequenceRandomSource();
        
        bool result = teacher.WillAsk(1, history, random);
        
        Assert.Equal(sourceAsked, result);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public void Xor_ReturnsExpected(bool sourceAAsked, bool sourceBAsked, bool expected)
    {
      
        var teacher = new Teacher(Subject.Calculus, TeacherRule.XorAandB, Subject.LinearAlgebra,
            Subject.Physics);
        var yesterday = new DayResult(0);
        yesterday.Add(Subject.LinearAlgebra, new LessonResult(true, sourceAAsked));
        yesterday.Add(Subject.Physics, new LessonResult(true, sourceBAsked));
        var history = new StudentHistory();
        history.Add(yesterday);
        var random = new SequenceRandomSource();

      
        bool result = teacher.WillAsk(1, history, random);
       
        Assert.Equal(expected, result);
    }
}
