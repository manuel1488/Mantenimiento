namespace App.Shared.Services.Implementation;

public class DateTimeService : IDateTime
{
    public DateTime Now => DateTime.UtcNow;

    public DateTime ToUtc(DateTime dateTime, TimeZoneInfo timeZone)
    {
        if (dateTime.Kind == DateTimeKind.Utc)
            return dateTime;

        var sourceDate = DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(sourceDate, timeZone);
    }

    public string FormatToTimezone(DateTime utcDate, TimeZoneInfo timeZoneInfo)
    {
        var convertedTime = TimeZoneInfo.ConvertTimeFromUtc(utcDate, timeZoneInfo);
        return convertedTime.ToString("g");
    }
}