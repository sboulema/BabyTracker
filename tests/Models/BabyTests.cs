using System;
using BabyTracker.Models.Database;
using NodaTime;
using NUnit.Framework;

namespace BabyTracker.Tests.Models;

[TestFixture]
public class BabyTests
{
    [Test]
    public void AgeInMonths_WithRecentBirth_ReturnsZero()
    {
        // Set DOB to today
        long todayTimestamp = DateTime.UtcNow.ToUnixTimestamp();
        var baby = new Baby
        {
            DOB = todayTimestamp
        };

        int ageInMonths = baby.AgeInMonths();

        Assert.That(ageInMonths, Is.EqualTo(0));
    }

    [Test]
    public void AgeInMonths_WithBirthOneMonthAgo_ReturnsOne()
    {
        // Set DOB to one month ago
        DateTime oneMonthAgo = DateTime.UtcNow.AddMonths(-1);
        var baby = new Baby
        {
            DOB = oneMonthAgo.ToUnixTimestamp()
        };

        int ageInMonths = baby.AgeInMonths();

        Assert.That(ageInMonths, Is.EqualTo(1));
    }

    [Test]
    public void AgeInMonths_WithBirthSixMonthsAgo_ReturnsSix()
    {
        // Set DOB to six months ago
        DateTime sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
        var baby = new Baby
        {
            DOB = sixMonthsAgo.ToUnixTimestamp()
        };

        int ageInMonths = baby.AgeInMonths();

        Assert.That(ageInMonths, Is.EqualTo(6));
    }

    [Test]
    public void AgeInMonths_WithBirthOneYearAgo_ReturnsTwelve()
    {
        // Set DOB to one year ago
        DateTime oneYearAgo = DateTime.UtcNow.AddYears(-1);
        var baby = new Baby
        {
            DOB = oneYearAgo.ToUnixTimestamp()
        };

        int ageInMonths = baby.AgeInMonths();

        Assert.That(ageInMonths, Is.EqualTo(12));
    }

    [Test]
    public void Age_WithRecentBirth_ReturnsCorrectString()
    {
        // Set DOB to today
        long todayTimestamp = DateTime.UtcNow.ToUnixTimestamp();
        var baby = new Baby
        {
            DOB = todayTimestamp
        };

        string age = baby.Age();

        Assert.That(age, Does.Contain("0 months"));
    }

    [Test]
    public void Age_WithBirthOneMonthAndFiveDaysAgo_ReturnsCorrectString()
    {
        // Set DOB to one month and 5 days ago
        DateTime date = DateTime.UtcNow.AddMonths(-1).AddDays(-5);
        var baby = new Baby
        {
            DOB = date.ToUnixTimestamp()
        };

        string age = baby.Age();

        Assert.That(age, Does.Contain("1 months"));
        Assert.That(age, Does.Contain("5 days"));
    }

    [Test]
    public void AgeInMonthsAt_WithBirthTimestampAndMoment_ReturnsFractionalMonths()
    {
        // Set DOB to a specific date
        DateTime dob = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var baby = new Baby
        {
            DOB = dob.ToUnixTimestamp()
        };

        // Check at a moment 1.5 months later
        DateTime moment = new DateTime(2024, 2, 15, 0, 0, 0, DateTimeKind.Utc);
        Instant instant = Instant.FromDateTimeUtc(moment);

        double ageInMonths = baby.AgeInMonthsAt(instant);

        // Should be approximately 1.5 months
        Assert.That(ageInMonths, Is.GreaterThan(1.4));
        Assert.That(ageInMonths, Is.LessThan(1.6));
    }

    [Test]
    public void AgeInMonthsAt_WithBirthTimestampAndSameMoment_ReturnsZero()
    {
        DateTime dob = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var baby = new Baby
        {
            DOB = dob.ToUnixTimestamp()
        };

        Instant instant = Instant.FromDateTimeUtc(dob);

        double ageInMonths = baby.AgeInMonthsAt(instant);

        Assert.That(ageInMonths, Is.EqualTo(0));
    }

    [Test]
    public void AgeInMonthsAt_WithBirthTimestampAndOneYearLater_ReturnsTwelve()
    {
        DateTime dob = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var baby = new Baby
        {
            DOB = dob.ToUnixTimestamp()
        };

        DateTime oneYearLater = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        Instant instant = Instant.FromDateTimeUtc(oneYearLater);

        double ageInMonths = baby.AgeInMonthsAt(instant);

        Assert.That(ageInMonths, Is.EqualTo(12));
    }

    [Test]
    public void Baby_WithAllPropertiesSet_ReturnsCorrectValues()
    {
        var baby = new Baby
        {
            Timestamp = 1234567890,
            Name = "Test Baby",
            DOB = 1705320000,
            DueDate = 1705320000,
            Gender = 1,
            PictureFileName = "test.jpg"
        };

        Assert.That(baby.Timestamp, Is.EqualTo(1234567890));
        Assert.That(baby.Name, Is.EqualTo("Test Baby"));
        Assert.That(baby.DOB, Is.EqualTo(1705320000));
        Assert.That(baby.DueDate, Is.EqualTo(1705320000));
        Assert.That(baby.Gender, Is.EqualTo(1));
        Assert.That(baby.PictureFileName, Is.EqualTo("test.jpg"));
    }

    [Test]
    public void Baby_WithDefaultValues_ReturnsEmptyStrings()
    {
        var baby = new Baby();

        Assert.That(baby.Name, Is.EqualTo(string.Empty));
        Assert.That(baby.PictureFileName, Is.EqualTo(string.Empty));
    }
}
