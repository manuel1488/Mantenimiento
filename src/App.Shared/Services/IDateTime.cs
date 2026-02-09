namespace App.Shared.Services;

public interface IDateTime
{
    DateTime Now { get; }
    DateTime ToUtc(DateTime dateTime, TimeZoneInfo timeZone);
    string FormatToTimezone(DateTime utcDate, TimeZoneInfo timeZoneInfo);
}