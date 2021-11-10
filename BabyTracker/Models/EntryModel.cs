using System;
using TimeZoneConverter;

namespace BabyTracker.Models;

public class EntryModel
{
    public string BabyName { get; set; } = string.Empty;

    public DateTime TimeUTC { get; set; }

    public DateTime Time
        => TimeZoneInfo.ConvertTimeFromUtc(TimeUTC, TZConvert.GetTimeZoneInfo("W. Europe Standard Time"));

    public string Note { get; set; } = string.Empty;
}
