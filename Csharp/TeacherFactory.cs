namespace Csharp;

internal sealed class TeacherFactory : ITeacherFactory
{
    private readonly IRandomSource _rand;

    public TeacherFactory(IRandomSource rand)
    {
        _rand = rand;
    }

    public Teacher[] CreateTeachers()
    {
        var subjects = Enum.GetValues<Subject>();
        var teachers = new Teacher[subjects.Length];

        for (var i = 0; i < subjects.Length; i++)
        {
            var rule = GetRandomRule(_rand);
            var dependencyA = GetRandomSubject(_rand, subjects);
            var dependencyB = GetRandomDifferentSubject(_rand, subjects, dependencyA);

            teachers[i] = new Teacher(subjects[i], rule, dependencyA, dependencyB);
        }
        return teachers;
    }

    private static Subject GetRandomDifferentSubject(IRandomSource rand, Subject[] subjects, Subject dependencyA)
    {
        var dependencyB = subjects[rand.Next(subjects.Length)];

        while (dependencyB == dependencyA)
        {
            dependencyB = subjects[rand.Next(subjects.Length)];
        }
        return dependencyB;
    }

    private static Subject GetRandomSubject(IRandomSource rand, Subject[] subjects)
    {
        var index = rand.Next(subjects.Length);
        return subjects[index];
    }

    private static TeacherRule GetRandomRule(IRandomSource rand)
    {
        var number = rand.Next(3);
        if (number == 0)
        {
            return TeacherRule.Check50;
        }
        if (number == 1)
        {
            return TeacherRule.DependA;
        }

        return TeacherRule.XorAandB;
    }
}
