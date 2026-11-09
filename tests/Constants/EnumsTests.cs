using BabyTracker.Constants;
using NUnit.Framework;

namespace BabyTracker.Tests.Constants;

[TestFixture]
public class EnumsTests
{
    [Test]
    public void DiaperStatusEnum_HasExpectedValues()
    {
        Assert.That((int)DiaperStatusEnum.Wet, Is.EqualTo(0));
        Assert.That((int)DiaperStatusEnum.Dirty, Is.EqualTo(1));
        Assert.That((int)DiaperStatusEnum.Mixed, Is.EqualTo(2));
    }

    [Test]
    public void DiaperStatusEnum_HasThreeValues()
    {
        var values = Enum.GetValues(typeof(DiaperStatusEnum));
        Assert.That(values.Length, Is.EqualTo(3));
    }

    [Test]
    public void ThemesEnum_HasExpectedValues()
    {
        Assert.That((int)ThemesEnum.Auto, Is.EqualTo(0));
        Assert.That((int)ThemesEnum.Light, Is.EqualTo(1));
        Assert.That((int)ThemesEnum.Dark, Is.EqualTo(2));
    }

    [Test]
    public void ThemesEnum_HasThreeValues()
    {
        var values = Enum.GetValues(typeof(ThemesEnum));
        Assert.That(values.Length, Is.EqualTo(3));
    }

    [Test]
    public void DiaperStatusEnum_StringValues()
    {
        Assert.That(DiaperStatusEnum.Wet.ToString(), Is.EqualTo("Wet"));
        Assert.That(DiaperStatusEnum.Dirty.ToString(), Is.EqualTo("Dirty"));
        Assert.That(DiaperStatusEnum.Mixed.ToString(), Is.EqualTo("Mixed"));
    }

    [Test]
    public void ThemesEnum_StringValues()
    {
        Assert.That(ThemesEnum.Auto.ToString(), Is.EqualTo("Auto"));
        Assert.That(ThemesEnum.Light.ToString(), Is.EqualTo("Light"));
        Assert.That(ThemesEnum.Dark.ToString(), Is.EqualTo("Dark"));
    }
}
