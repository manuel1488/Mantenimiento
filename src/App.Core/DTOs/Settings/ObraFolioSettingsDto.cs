namespace App.Core.DTOs.Settings;

public class ObraFolioSettingsDto
{
    public int Id { get; set; }
    public string FolioPrefijo { get; set; } = "OBR";
    public int FolioDigitos { get; set; } = 4;
}
