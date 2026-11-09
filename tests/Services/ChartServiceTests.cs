using System;
using BabyTracker.Services;
using NUnit.Framework;

namespace BabyTracker.Tests.Services;

[TestFixture]
public class ChartServiceTests
{
    [Test]
    public void GetRangeKey_WithAgeLessThan24Months_Returns0_2()
    {
        // Use reflection to call the private GetRangeKey method
        var method = typeof(ChartService).GetMethod("GetRangeKey", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        string result = (string)method?.Invoke(null, new object[] { 12.0 });
        
        Assert.That(result, Is.EqualTo("0_2"));
    }

    [Test]
    public void GetRangeKey_WithAge24Months_Returns2_5()
    {
        var method = typeof(ChartService).GetMethod("GetRangeKey", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        string result = (string)method?.Invoke(null, new object[] { 24.0 });
        
        Assert.That(result, Is.EqualTo("2_5"));
    }

    [Test]
    public void GetRangeKey_WithAge30Months_Returns2_5()
    {
        var method = typeof(ChartService).GetMethod("GetRangeKey", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        string result = (string)method?.Invoke(null, new object[] { 30.0 });
        
        Assert.That(result, Is.EqualTo("2_5"));
    }

    [Test]
    public void GetRangeKey_WithAge61Months_Returns5_19()
    {
        var method = typeof(ChartService).GetMethod("GetRangeKey", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        string result = (string)method?.Invoke(null, new object[] { 61.0 });
        
        Assert.That(result, Is.EqualTo("5_19"));
    }

    [Test]
    public void GetRangeKey_WithAge100Months_Returns5_19()
    {
        var method = typeof(ChartService).GetMethod("GetRangeKey", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        string result = (string)method?.Invoke(null, new object[] { 100.0 });
        
        Assert.That(result, Is.EqualTo("5_19"));
    }

    [Test]
    public void GetWeightRangeKey_WithAgeLessThan61Months_Returns0_5()
    {
        var method = typeof(ChartService).GetMethod("GetWeightRangeKey", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        string result = (string)method?.Invoke(null, new object[] { 30.0 });
        
        Assert.That(result, Is.EqualTo("0_5"));
    }

    [Test]
    public void GetWeightRangeKey_WithAge61Months_Returns5_10()
    {
        var method = typeof(ChartService).GetMethod("GetWeightRangeKey", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        string result = (string)method?.Invoke(null, new object[] { 61.0 });
        
        Assert.That(result, Is.EqualTo("5_10"));
    }

    [Test]
    public void GetWeightRangeKey_WithAge100Months_Returns5_10()
    {
        var method = typeof(ChartService).GetMethod("GetWeightRangeKey", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        string result = (string)method?.Invoke(null, new object[] { 100.0 });
        
        Assert.That(result, Is.EqualTo("5_10"));
    }

    [Test]
    public void GetRangeKey_WithAge0_Returns0_2()
    {
        var method = typeof(ChartService).GetMethod("GetRangeKey", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        string result = (string)method?.Invoke(null, new object[] { 0.0 });
        
        Assert.That(result, Is.EqualTo("0_2"));
    }

    [Test]
    public void GetRangeKey_WithNegativeAge_Returns0_2()
    {
        var method = typeof(ChartService).GetMethod("GetRangeKey", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        string result = (string)method?.Invoke(null, new object[] { -1.0 });
        
        Assert.That(result, Is.EqualTo("0_2"));
    }
}
