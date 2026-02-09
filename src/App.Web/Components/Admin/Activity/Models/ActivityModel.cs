namespace App.Web.Components.Admin.Activity.Models;

public class ActivityModel
{
    public DateTime Timestamp { get; set; }
    public string UserName { get; set; } = string.Empty;
    public ActivityType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
}