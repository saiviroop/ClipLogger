namespace ClipLogger.Core;

public static class CheckInScheduler
{
    public static bool IsDue(DateTime start, DateTime now, int intervalMinutes)
        => (now - start).TotalMinutes >= intervalMinutes;
}
