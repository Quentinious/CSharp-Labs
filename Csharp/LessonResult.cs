namespace Csharp;

internal sealed class LessonResult
{
    public bool Attended { get; }
    public bool WasActuallyAsked { get; }

    public LessonResult(bool attended, bool wasActuallyAsked)
    {
        Attended = attended;
        WasActuallyAsked = wasActuallyAsked;
    }
}
