using System.Text.Json.Serialization;

namespace BabyTracker.Models.Charts;

public class Point
{
    public Point(double x, double y)
    {
        X = x;
        Y = y;
    }

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }
}
