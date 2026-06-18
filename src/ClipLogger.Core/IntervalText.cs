namespace ClipLogger.Core;

public static class IntervalText
{
    public static string Describe(int minutes)
    {
        if (minutes % 60 == 0)
        {
            var hours = minutes / 60;
            return hours == 1 ? "1 hour" : $"{hours} hours";
        }
        return minutes == 1 ? "1 minute" : $"{minutes} minutes";
    }
}
