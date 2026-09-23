namespace Csharp;

class SemesterSimulator
{
    private Teacher[] teachers;
    private StudentStrategy strategy;
    private Random rand;

    public SemesterSimulator(Teacher[] teachers, StudentStrategy strategy, Random rand)
    {
        this.teachers = teachers;
        this.strategy = strategy;
        this.rand = rand;
    }

    public int Run()
    {
        bool[] yesterdayAsked = new bool[teachers.Length];
        int pleasure = 0;
        for (int day = 0; day < 100; day++)
        {
            bool[] todayAsked = new bool[teachers.Length];
            pleasure++;
            for (int i = 0; i < teachers.Length; i++)
            {
                Teacher teacher = teachers[i];
                bool skip = strategy.ShouldSkip(teacher, yesterdayAsked);
                bool asked = teacher.WillAsk(yesterdayAsked, rand);
                todayAsked[i] = asked;
                if (skip)
                {
                    pleasure++;
                    if (asked)
                    {
                        return 0;
                    }
                }
            }
            yesterdayAsked = todayAsked;
        }
        return pleasure;
    }
}
