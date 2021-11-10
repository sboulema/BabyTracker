using System.Collections.Generic;
using System.Text.Json;
using BabyTracker.Models.Charts;

namespace BabyTracker.Models.ViewModels;

public class ChartsViewModel : BaseViewModel
{
    public List<Point> WeightPoints { get; set; } = new();

    public string WeightPointsSD0 { get; set; } = string.Empty;

    public string WeightPointsSD2 { get; set; } = string.Empty;

    public string WeightPointsSD2neg { get; set; } = string.Empty;

    public List<Point> LengthPoints { get; set; } = new();

    public string LengthPointsSD0 { get; set; } = string.Empty;

    public string LengthPointsSD2 { get; set; } = string.Empty;

    public string LengthPointsSD2neg { get; set; } = string.Empty;

    public List<Point> HeadSizePoints { get; set; } = new();

    public string HeadSizePointsSD0 { get; set; } = string.Empty;

    public string HeadSizePointsSD2 { get; set; } = string.Empty;

    public string HeadSizePointsSD2neg { get; set; } = string.Empty;

    public List<Point> BMIPoints { get; set; } = new();

    public string BMIPointsSD0 { get; set; } = string.Empty;

    public string BMIPointsSD2 { get; set; } = string.Empty;

    public string BMIPointsSD2neg { get; set; } = string.Empty;


    public string WeightPointsAsJson => JsonSerializer.Serialize(WeightPoints);

    public string LengthPointsAsJson => JsonSerializer.Serialize(LengthPoints);

    public string HeadSizePointsAsJson => JsonSerializer.Serialize(HeadSizePoints);

    public string BMIPointsAsJson => JsonSerializer.Serialize(BMIPoints);
}
