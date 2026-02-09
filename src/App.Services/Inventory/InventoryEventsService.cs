// App.Web.Services/InventoryEventsService.cs
namespace App.Web.Services;

public class InventoryEventsService
{
    public event EventHandler? AlertsChanged;

    public void NotifyAlertsChanged()
    {
        AlertsChanged?.Invoke(this, EventArgs.Empty);
    }
}