using System;
using BabyTracker.Extensions;
using NUnit.Framework;

namespace BabyTracker.Tests.Extensions;

[TestFixture]
public class TimeExtensionsTests
{
    [Test]
    public void ToDateTimeUTC_WithValidTimestamp_ReturnsCorrectDateTime()
    {
        // Unix timestamp for 2024-01-15 12:00:00 UTC
        long timestamp = 1705320000;
        DateTime expected = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        
        DateTime result = timestamp.ToDateTimeUTC();
        
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void ToDateTimeUTC_WithZeroTimestamp_ReturnsUnixEpoch()
    {
        long timestamp = 0;
        DateTime expected = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        DateTime result = timestamp.ToDateTimeUTC();
        
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void ToDateTimeLocal_WithValidTimestamp_ReturnsDateTime()
    {
        // Unix timestamp for 2024-01-15 12:00:00 UTC
        long timestamp = 1705320000;
        
        // This test just verifies the method doesn't throw
        // The actual time conversion depends on the timezone database
        // which may differ between platforms
        DateTime result = timestamp.ToDateTimeLocal();
        
        Assert.That(result, Is.Not.EqualTo(default(DateTime)));
    }

    [Test]
    public void ToDateOnly_WithValidTimestamp_ReturnsCorrectDate()
    {
        // Unix timestamp for 2024-01-15 12:00:00 UTC
        long timestamp = 1705320000;
        DateOnly expected = new DateOnly(2024, 1, 15);
        
        DateOnly result = timestamp.ToDateOnly();
        
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void ToUnixTimestamp_WithValidDateTime_ReturnsCorrectTimestamp()
    {
        DateTime dateTime = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        long expected = 1705320000;
        
        long result = dateTime.ToUnixTimestamp();
        
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void ToUnixTimestamp_WithLocalDateTime_ConvertsToUTC()
    {
        // Create a local datetime (kind = Local)
        DateTime localDateTime = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Local);
        
        long result = localDateTime.ToUnixTimestamp();
        
        // The timestamp should be based on the UTC equivalent
        // This test just verifies it doesn't throw and returns a reasonable value
        Assert.That(result, Is.GreaterThan(0));
    }

    [Test]
    public void RoundTrip_ToUnixTimestamp_ToDateTimeUTC()
    {
        DateTime original = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        
        long timestamp = original.ToUnixTimestamp();
        DateTime result = timestamp.ToDateTimeUTC();
        
        Assert.That(result, Is.EqualTo(original));
    }

    [Test]
    public void RoundTrip_ToDateTimeUTC_ToUnixTimestamp()
    {
        long original = 1705320000;
        
        DateTime dateTime = original.ToDateTimeUTC();
        long result = dateTime.ToUnixTimestamp();
        
        Assert.That(result, Is.EqualTo(original));
    }
}
