namespace App.Core.Models;

public class CameraDimension
{
    public CameraDimension()
    {
        
    }

    public CameraDimension(int min, int ideal, int max)
    {
        Min = min;
        Ideal = ideal;
        Max = max;
    }

    public int Min { get; set; }
    public int Ideal { get; set; }
    public int Max { get; set; }
}