using System;
using TimeZoneConverter;

public class PictureModel
{
    public string Filename { get; set; }

    public DateTime TimeUTC { get; set; }

    public DateTime Time
    {
        get 
        {
            return TimeZoneInfo.ConvertTimeFromUtc(TimeUTC, TZConvert.GetTimeZoneInfo("W. Europe Standard Time"));
        }
    }
}