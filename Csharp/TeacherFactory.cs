namespace Csharp;

static class TeacherFactory
{
    public static Teacher[] CreateTeachers(Random rand)
    {
        Subject[] subjects = Enum.GetValues<Subject>();

        Teacher[] teachers = new Teacher[subjects.Length];

        for (int i = 0; i < subjects.Length; i++)
        {
            TeacherRule rule = GetRandomRule(rand);

            Subject dependencyA = GetRandomSubject(rand, subjects);

            Subject dependencyB =
                GetRandomDifferentSubject(rand, subjects, dependencyA);

            teachers[i] = new Teacher(
                subjects[i],
                rule,
                dependencyA,
                dependencyB
            );
        }

        return teachers;
    } 
    private static Subject GetRandomDifferentSubject(Random rand, Subject[] subjects, Subject dependencyA)
    {
        Subject dependencyB = subjects[rand.Next(subjects.Length)];

        while (dependencyB == dependencyA)
        {
            dependencyB = subjects[rand.Next(subjects.Length)];
        }
        return dependencyB;
    }

    private static Subject GetRandomSubject(Random rand, Subject[] subjects)
    {
        int index =  rand.Next(subjects.Length);
        return subjects[index];
    }

    private static TeacherRule GetRandomRule(Random rand)
    {
        int number = rand.Next(3);
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