namespace Csharp;

enum TeacherRule
{
    Check50,
    DependA,
    XorAandB
}

class Teacher
{
    public Subject Subject { get; }
    public TeacherRule Rule { get; }

    public Subject DependA { get; }
    public Subject DependB { get; }

    public Teacher(Subject subject, TeacherRule rule, Subject dependA, Subject dependB)
    {
        Subject = subject;
        Rule = rule;
        DependA = dependA;
        DependB = dependB;
    }
    public double GetAskProbability(int day, StudentHistory history)
    {
        if (Rule == TeacherRule.Check50)
        {
            return 0.5;
        }

        if (day == 0)
        {
            return 0.0;
        }

        if (Rule == TeacherRule.DependA)
        {
            var askedA = history.WasActuallyAsked(day - 1, DependA);
            if (askedA)
            {
                return 1.0;
            }

            return 0.0;
        }

        var askedDependA = history.WasActuallyAsked(day - 1, DependA);
        var askedDependB = history.WasActuallyAsked(day - 1, DependB);
        return askedDependA != askedDependB ? 1.0 : 0.0;
    }

    public bool WillAsk(int day, StudentHistory history, IRandomSource rand)
    {
        var probability = GetAskProbability(day, history);

        if (probability == 0.0)
        {
            return false;
        }
        
        if (Math.Abs(probability - 1.0) < 0.001)
        {
            return true;
        }

        return rand.Next(2) == 0;
    }
}
