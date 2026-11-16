using System.Security.Cryptography;

namespace Csharp;

// SplitMix64 v1: the entire generator state is one 64-bit integer.
// This implementation does not depend on System.Random's internal algorithm.
internal sealed class StatefulRandomSource : IRandomSource
{
    public long State { get; private set; }

    public StatefulRandomSource(long state)
    {
        State = state;
    }

    public static StatefulRandomSource Create()
    {
        return new StatefulRandomSource(BitConverter.ToInt64(RandomNumberGenerator.GetBytes(8)));
    }

    public int Next(int maxValue)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxValue);
        var bound = (ulong)maxValue;
        // Rejection sampling avoids modulo bias.
        var threshold = unchecked(0UL - bound) % bound;
        ulong value;
        do
        {
            value = NextValue();
        } while (value < threshold);

        return (int)(value % bound);
    }

    private ulong NextValue()
    {
        unchecked
        {
            var value = (ulong)State + 0x9E3779B97F4A7C15UL;
            State = (long)value;
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
            return value ^ (value >> 31);
        }
    }
}
