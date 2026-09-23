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
    public double GetAskProbability(bool[] yesterdayAsked)
    {
        if (Rule == TeacherRule.Check50)
        {
            return 0.5;
        }
        if (Rule == TeacherRule.DependA)
        {
            bool askedA = WasAskedYesterday(DependA, yesterdayAsked);
            if (askedA)
            {
                return 1.0;
            }

            return 0.0;
        }
        
        bool askedDependA = WasAskedYesterday(DependA, yesterdayAsked);
        bool askedDependB = WasAskedYesterday(DependB, yesterdayAsked);

        if (askedDependA && !askedDependB)
        {
            return 1.0;
        }

        if (!askedDependA && askedDependB)
        {
            return 1.0;
        }

        return 0.0;
    }

    private bool WasAskedYesterday(Subject subject, bool[] yesterdayAsked)
    {
        int index = (int)subject;
        return yesterdayAsked[index];
    }
    public bool WillAsk(bool[] yesterdayAsked, Random rand)
    {
        double probability = GetAskProbability(yesterdayAsked);

        if (probability == 0.0)
        {
            return false;
        }

        if (probability == 1.0)
        {
            return true;
        }
        return rand.Next(2) == 0;
    }
}