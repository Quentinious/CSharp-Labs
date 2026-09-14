namespace Csharp;

class StudentStrategy
{
    public bool ShouldSkip(
        Teacher teacher,
        bool[] yesterdayAsked)
    {
        double probability =
            teacher.GetAskProbability(yesterdayAsked);

        if (probability == 0.0)
        {
            return true;
        }

        return false;
    }
}