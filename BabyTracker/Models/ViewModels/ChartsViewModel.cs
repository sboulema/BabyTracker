using System.Collections.Generic;
using System.Text.Json;
using BabyTracker.Models.Charts;

namespace BabyTracker.Models.ViewModels
{
    public class ChartsViewModel : BaseViewModel
    {
        public List<Point> WeightPoints { get; set; } = new ();

        public List<Point> WeightPointsMedian { get; set; } = new ();

        public List<Point> LengthPoints { get; set; } = new ();

        public List<Point> HeadSizePoints { get; set; } = new ();

        public List<Point> BMIPoints { get; set; } = new ();

        public string WeightPointsAsJson => JsonSerializer.Serialize(WeightPoints);
        public string WeightPointsMedianAsJson => JsonSerializer.Serialize(WeightPointsMedian);

        public string LengthPointsAsJson => JsonSerializer.Serialize(LengthPoints);

        public string HeadSizePointsAsJson => JsonSerializer.Serialize(HeadSizePoints);

        public string BMIPointsAsJson => JsonSerializer.Serialize(BMIPoints);
    }
}
