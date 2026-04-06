namespace App.Shared.Services;

public interface IDateTime
{
    DateTime Now { get; }
    DateTime ToUtc(DateTime dateTime, TimeZoneInfo timeZone);
    DateTime ConvertToTimezone(DateTime utcDate, TimeZoneInfo timeZoneInfo);
    string FormatToTimezone(DateTime utcDate, TimeZoneInfo timeZoneInfo);
}