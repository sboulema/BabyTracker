using System;
using TimeZoneConverter;

namespace BabyTracker.Models;

public class EntryModel
{
    public string BabyName { get; set; }

    public DateTime TimeUTC { get; set; }

    public DateTime Time {
        get {
            return TimeZoneInfo.ConvertTimeFromUtc(TimeUTC, TZConvert.GetTimeZoneInfo("W. Europe Standard Time"));
        }
    }

    public string Note { get; set; }
}
