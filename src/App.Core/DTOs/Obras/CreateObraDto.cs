namespace App.Core.DTOs.Obras;

public class CreateObraDto
{
    public int ClienteId { get; set; }
    public string Direccion { get; set; } = null!;
    public bool Urgente { get; set; }
}
