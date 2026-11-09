using BabyTracker.Models.Charts;
using NUnit.Framework;

namespace BabyTracker.Tests.Models.Charts;

[TestFixture]
public class PointTests
{
    [Test]
    public void Point_WithConstructor_SetsProperties()
    {
        var point = new Point(1.5, 2.5);

        Assert.That(point.X, Is.EqualTo(1.5));
        Assert.That(point.Y, Is.EqualTo(2.5));
    }

    [Test]
    public void Point_WithNegativeValues_HandlesCorrectly()
    {
        var point = new Point(-1.5, -2.5);

        Assert.That(point.X, Is.EqualTo(-1.5));
        Assert.That(point.Y, Is.EqualTo(-2.5));
    }

    [Test]
    public void Point_WithZeroValues_HandlesCorrectly()
    {
        var point = new Point(0, 0);

        Assert.That(point.X, Is.EqualTo(0));
        Assert.That(point.Y, Is.EqualTo(0));
    }

    [Test]
    public void Point_WithLargeValues_HandlesCorrectly()
    {
        var point = new Point(1000000, 2000000);

        Assert.That(point.X, Is.EqualTo(1000000));
        Assert.That(point.Y, Is.EqualTo(2000000));
    }

    [Test]
    public void Point_WithDecimalValues_HandlesCorrectly()
    {
        var point = new Point(1.234567, 2.345678);

        Assert.That(point.X, Is.EqualTo(1.234567).Within(0.000001));
        Assert.That(point.Y, Is.EqualTo(2.345678).Within(0.000001));
    }

}
