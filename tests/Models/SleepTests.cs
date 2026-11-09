using BabyTracker.Models.Database;
using NUnit.Framework;

namespace BabyTracker.Tests.Models;

[TestFixture]
public class SleepTests
{
    [Test]
    public void DurationText_WithDurationLessThan60Minutes_ReturnsMinutes()
    {
        var sleep = new Sleep
        {
            Duration = 30
        };

        Assert.That(sleep.DurationText, Is.EqualTo("30 min"));
    }

    [Test]
    public void DurationText_WithDurationExactly60Minutes_ReturnsHours()
    {
        var sleep = new Sleep
        {
            Duration = 60
        };

        Assert.That(sleep.DurationText, Is.EqualTo("1 hrs 0 min"));
    }

    [Test]
    public void DurationText_WithDuration90Minutes_ReturnsHoursAndMinutes()
    {
        var sleep = new Sleep
        {
            Duration = 90
        };

        Assert.That(sleep.DurationText, Is.EqualTo("1 hrs 30 min"));
    }

    [Test]
    public void DurationText_WithDuration120Minutes_ReturnsTwoHours()
    {
        var sleep = new Sleep
        {
            Duration = 120
        };

        Assert.That(sleep.DurationText, Is.EqualTo("2 hrs 0 min"));
    }

    [Test]
    public void DurationText_WithDuration150Minutes_ReturnsTwoHoursThirtyMinutes()
    {
        var sleep = new Sleep
        {
            Duration = 150
        };

        Assert.That(sleep.DurationText, Is.EqualTo("2 hrs 30 min"));
    }

    [Test]
    public void DurationText_WithZeroDuration_ReturnsZeroMinutes()
    {
        var sleep = new Sleep
        {
            Duration = 0
        };

        Assert.That(sleep.DurationText, Is.EqualTo("0 min"));
    }

    [Test]
    public void DurationText_WithLargeDuration_ReturnsCorrectFormat()
    {
        var sleep = new Sleep
        {
            Duration = 1439 // 23 hours and 59 minutes
        };

        Assert.That(sleep.DurationText, Is.EqualTo("23 hrs 59 min"));
    }
}
