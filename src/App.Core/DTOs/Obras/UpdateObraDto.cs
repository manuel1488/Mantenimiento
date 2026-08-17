namespace App.Core.DTOs.Obras;

public class UpdateObraDto
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public string Direccion { get; set; } = null!;
    public bool Urgente { get; set; }
}
