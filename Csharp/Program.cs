using System;
using Csharp;

enum Subject
{
    Math,
    LinearAlg,
    DiscreteMath,
    Programming,
    Physics,
    English
}

class Program
{
    static void Main()
    {
        Random rand = new Random();

        Teacher[] teachers = TeacherFactory.CreateTeachers(rand);
        
        
        Console.WriteLine();
        StudentStrategy strategy = new StudentStrategy();
        SemesterSimulator simulator = new SemesterSimulator(teachers, strategy, rand);
        
        int totalPleasure = simulator.Run();
        double averagePleasure = (double)totalPleasure / 100;
        Console.WriteLine($"Общее удовольствие: {totalPleasure}");
        Console.WriteLine($"Среднее удовольствие в день: {averagePleasure}");
    }
    
}