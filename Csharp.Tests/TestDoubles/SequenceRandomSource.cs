namespace Csharp.Tests.TestDoubles;

internal sealed class SequenceRandomSource : IRandomSource
{
    private readonly Queue<int> _values;

    public SequenceRandomSource(params int[] values)
    {
        _values = new Queue<int>(values);
    }

    public int Remaining
    {
        get
        {
            return _values.Count;
        }
    }

    public int Next(int maxValue)
    {
        var value = _values.Dequeue();
        return value;
    }
}
