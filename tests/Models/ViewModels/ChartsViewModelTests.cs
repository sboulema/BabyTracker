using BabyTracker.Models.Charts;
using BabyTracker.Models.ViewModels;
using NUnit.Framework;

namespace BabyTracker.Tests.Models.ViewModels;

[TestFixture]
public class ChartsViewModelTests
{
    [Test]
    public void ChartsViewModel_DefaultConstructor_InitializesEmptyLists()
    {
        var viewModel = new ChartsViewModel();

        Assert.That(viewModel.WeightPoints, Is.Not.Null);
        Assert.That(viewModel.WeightPoints, Is.Empty);
        
        Assert.That(viewModel.LengthPoints, Is.Not.Null);
        Assert.That(viewModel.LengthPoints, Is.Empty);
        
        Assert.That(viewModel.HeadSizePoints, Is.Not.Null);
        Assert.That(viewModel.HeadSizePoints, Is.Empty);
        
        Assert.That(viewModel.BMIPoints, Is.Not.Null);
        Assert.That(viewModel.BMIPoints, Is.Empty);
    }

    [Test]
    public void ChartsViewModel_WithData_RetainsValues()
    {
        var viewModel = new ChartsViewModel
        {
            WeightPoints = { new Point(1, 10), new Point(2, 12) },
            LengthPoints = { new Point(1, 75), new Point(2, 80) },
            HeadSizePoints = { new Point(1, 45), new Point(2, 47) },
            BMIPoints = { new Point(1, 18.5), new Point(2, 19.0) }
        };

        Assert.That(viewModel.WeightPoints, Has.Count.EqualTo(2));
        Assert.That(viewModel.LengthPoints, Has.Count.EqualTo(2));
        Assert.That(viewModel.HeadSizePoints, Has.Count.EqualTo(2));
        Assert.That(viewModel.BMIPoints, Has.Count.EqualTo(2));
    }

    [Test]
    public void ChartsViewModel_StringProperties_AreNullByDefault()
    {
        var viewModel = new ChartsViewModel();

        Assert.That(viewModel.WeightPointsSD0, Is.Null);
        Assert.That(viewModel.WeightPointsSD2, Is.Null);
        Assert.That(viewModel.WeightPointsSD2neg, Is.Null);
        
        Assert.That(viewModel.LengthPointsSD0, Is.Null);
        Assert.That(viewModel.LengthPointsSD2, Is.Null);
        Assert.That(viewModel.LengthPointsSD2neg, Is.Null);
        
        Assert.That(viewModel.HeadSizePointsSD0, Is.Null);
        Assert.That(viewModel.HeadSizePointsSD2, Is.Null);
        Assert.That(viewModel.HeadSizePointsSD2neg, Is.Null);
        
        Assert.That(viewModel.BMIPointsSD0, Is.Null);
        Assert.That(viewModel.BMIPointsSD2, Is.Null);
        Assert.That(viewModel.BMIPointsSD2neg, Is.Null);
    }

    [Test]
    public void ChartsViewModel_WithStringProperties_RetainsValues()
    {
        var viewModel = new ChartsViewModel
        {
            WeightPointsSD0 = "[1, 2, 3]",
            WeightPointsSD2 = "[4, 5, 6]",
            WeightPointsSD2neg = "[7, 8, 9]"
        };

        Assert.That(viewModel.WeightPointsSD0, Is.EqualTo("[1, 2, 3]"));
        Assert.That(viewModel.WeightPointsSD2, Is.EqualTo("[4, 5, 6]"));
        Assert.That(viewModel.WeightPointsSD2neg, Is.EqualTo("[7, 8, 9]"));
    }
}
