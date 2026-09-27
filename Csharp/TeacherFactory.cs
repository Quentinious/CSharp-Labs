namespace Csharp;

static class TeacherFactory
{
    public static Teacher[] CreateTeachers(Random rand)
    {
        var subjects = Enum.GetValues<Subject>();
        var teachers = new Teacher[subjects.Length];

        for (var i = 0; i < subjects.Length; i++)
        {
            var rule = GetRandomRule(rand);
            var dependencyA = GetRandomSubject(rand, subjects);
            var dependencyB = GetRandomDifferentSubject(rand, subjects, dependencyA);

            teachers[i] = new Teacher(subjects[i], rule, dependencyA, dependencyB);
        }
        return teachers;
    }

    private static Subject GetRandomDifferentSubject(Random rand, Subject[] subjects, Subject dependencyA)
    {
        var dependencyB = subjects[rand.Next(subjects.Length)];

        while (dependencyB == dependencyA)
        {
            dependencyB = subjects[rand.Next(subjects.Length)];
        }
        return dependencyB;
    }

    private static Subject GetRandomSubject(Random rand, Subject[] subjects)
    {
        var index = rand.Next(subjects.Length);
        return subjects[index];
    }

    private static TeacherRule GetRandomRule(Random rand)
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