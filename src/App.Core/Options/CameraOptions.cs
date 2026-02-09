using App.Core.Models;

namespace App.Core.Options;

public class CameraOptions
{
    public const string SectionName = "Camera";
    public CameraDimension Width { get; set; } = new CameraDimension(min: 1024, ideal: 1280, max: 1920);
    public CameraDimension Height { get; set; } = new CameraDimension(min: 720, ideal: 776, max: 1080);
    public string MimeType { get; set; } = "image/jpeg";
    public double Quality { get; set; } = 1.0;
    public string FacingMode { get; set; } = "environment";
}