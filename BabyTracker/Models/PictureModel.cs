using System;
using TimeZoneConverter;

namespace BabyTracker.Models;

public class PictureModel
{
    public string Filename { get; set; } = string.Empty;

    public DateTime TimeUTC { get; set; }

    public DateTime Time
        => TimeZoneInfo.ConvertTimeFromUtc(TimeUTC, TZConvert.GetTimeZoneInfo("W. Europe Standard Time"));
}
