using System;

namespace Csharp;

public sealed class StudentStrategy : ISkipStrategy
{
    private const int DefaultObservationDays = 10;
    private const double DefaultRiskThreshold = 0.001;

    private readonly int _subjectCount;
    private readonly int _observationDays;
    private readonly double _riskThreshold;

    private readonly double[] _check50Weights;
    private readonly double[,] _dependWeights;
    private readonly double[,,] _xorWeights;

    private int _processedDays;

    public StudentStrategy() : this(DefaultObservationDays, DefaultRiskThreshold)
    {
    }

    public StudentStrategy(int observationDays, double riskThreshold)
    {
        _subjectCount = Enum.GetValues<Subject>().Length;
        _observationDays = observationDays;
        _riskThreshold = riskThreshold;

        _check50Weights = new double[_subjectCount];
        _dependWeights = new double[_subjectCount, _subjectCount];
        _xorWeights = new double[_subjectCount, _subjectCount, _subjectCount];

        InitializeWeights();
    }

    public string Name => "Alexeev 23205 Strategy";

    public bool[] DecideDay(int day, IReadOnlyStudentHistory history)
    {
        ProcessNewHistory(day, history);

        var attendance = new bool[_subjectCount];

        if (day < _observationDays)
        {
            for (var i = 0; i < attendance.Length; i++)
            {
                attendance[i] = true;
            }

            return attendance;
        }

        var previousResults = GetPreviousResults(day, history);

        for (var i = 0; i < attendance.Length; i++)
        {
            var askProbability = PredictAskProbability(i, previousResults);
            attendance[i] = askProbability >= _riskThreshold;
        }

        return attendance;
    }

    private void InitializeWeights()
    {
        var dependWeight = 1.0 / (3.0 * _subjectCount);
        var pairCount = _subjectCount * (_subjectCount - 1) / 2;
        var xorWeight = 1.0 / (3.0 * pairCount);

        for (var target = 0; target < _subjectCount; target++)
        {
            _check50Weights[target] = 1.0 / 3.0;

            for (var source = 0; source < _subjectCount; source++)
            {
                _dependWeights[target, source] = dependWeight;
            }

            for (var first = 0; first < _subjectCount; first++)
            {
                for (var second = first + 1; second < _subjectCount; second++)
                {
                    _xorWeights[target, first, second] = xorWeight;
                }
            }
        }
    }

    private void ProcessNewHistory(int currentDay, IReadOnlyStudentHistory history)
    {
        while (_processedDays < currentDay)
        {
            UpdateWeights(_processedDays, history);
            _processedDays++;
        }
    }

    private void UpdateWeights(int day, IReadOnlyStudentHistory history)
    {
        var previousResults = GetPreviousResults(day, history);

        for (var target = 0; target < _subjectCount; target++)
        {
            var subject = (Subject)target;
            var asked = GetKnownResult(day, subject, history);

            _check50Weights[target] *= 0.5;

            for (var source = 0; source < _subjectCount; source++)
            {
                if (previousResults[source] != asked)
                {
                    _dependWeights[target, source] = 0.0;
                }
            }

            for (var first = 0; first < _subjectCount; first++)
            {
                for (var second = first + 1; second < _subjectCount; second++)
                {
                    var prediction = previousResults[first] != previousResults[second];
                    if (prediction != asked)
                    {
                        _xorWeights[target, first, second] = 0.0;
                    }
                }
            }

            NormalizeWeights(target);
        }
    }

    private void NormalizeWeights(int target)
    {
        var total = _check50Weights[target];

        for (var source = 0; source < _subjectCount; source++)
        {
            total += _dependWeights[target, source];
        }

        for (var first = 0; first < _subjectCount; first++)
        {
            for (var second = first + 1; second < _subjectCount; second++)
            {
                total += _xorWeights[target, first, second];
            }
        }

        _check50Weights[target] /= total;

        for (var source = 0; source < _subjectCount; source++)
        {
            _dependWeights[target, source] /= total;
        }

        for (var first = 0; first < _subjectCount; first++)
        {
            for (var second = first + 1; second < _subjectCount; second++)
            {
                _xorWeights[target, first, second] /= total;
            }
        }
    }

    private double PredictAskProbability(int target, bool[] previousResults)
    {
        var probability = _check50Weights[target] * 0.5;

        for (var source = 0; source < _subjectCount; source++)
        {
            if (previousResults[source])
            {
                probability += _dependWeights[target, source];
            }
        }

        for (var first = 0; first < _subjectCount; first++)
        {
            for (var second = first + 1; second < _subjectCount; second++)
            {
                if (previousResults[first] != previousResults[second])
                {
                    probability += _xorWeights[target, first, second];
                }
            }
        }

        return probability;
    }

    private bool[] GetPreviousResults(int day, IReadOnlyStudentHistory history)
    {
        var results = new bool[_subjectCount];
        if (day == 0)
        {
            return results;
        }

        for (var source = 0; source < _subjectCount; source++)
        {
            var subject = (Subject)source;
            results[source] = GetKnownResult(day - 1, subject, history);
        }

        return results;
    }

    private static bool GetKnownResult(int day, Subject subject, IReadOnlyStudentHistory history)
    {
        var observed = history.WasAsked(day, subject);
        if (observed.HasValue)
        {
            return observed.Value;
        }
        
        return false;
    }
}
