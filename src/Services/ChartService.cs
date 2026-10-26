using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using BabyTracker.Extensions;
using BabyTracker.Models.Charts;
using BabyTracker.Models.Database;
using BabyTracker.Models.ViewModels;
using ClosedXML.Excel;
using NodaTime;

namespace BabyTracker.Services;

public interface IChartService
{
    Task<ChartsViewModel> GetViewModel(ClaimsPrincipal user, string babyName, int? maxAge = null);
}

public class ChartService(ISqLiteService sqLiteService) : IChartService
{
    private readonly static ConcurrentDictionary<string, List<ExcelRow>> ReferenceCache = new();

    public async Task<ChartsViewModel> GetViewModel(ClaimsPrincipal user, string babyName, int? maxAge = null)
    {
        var viewModel = new ChartsViewModel();

        sqLiteService.OpenDataConnection(user);

        var entries = new List<Growth>();
        var babies = new List<Baby>();

        try
        {
            entries = await sqLiteService.GetGrowth(long.MinValue, long.MaxValue, babyName);
            babies = await sqLiteService.GetBabiesFromDb();
        }
        finally
        {
            await sqLiteService.CloseDataConnection();
        }

        var baby = babies.FirstOrDefault(baby => baby.Name == babyName);

        if (baby == null)
        {
            return viewModel;
        }

        var babyRangeKey = GetRangeKey(baby.AgeInMonths());
        var babyWeightKey = GetWeightRangeKey(baby.AgeInMonths());

        var entryAges = entries
            .Select(entry => new {
                Entry = entry,
                Age = baby.AgeInMonthsAt(Instant.FromUnixTimeSeconds(entry.Time))
            })
            .Where(item => maxAge == null || item.Age <= maxAge);

        foreach (var item in entryAges)
        {
            var entry = item.Entry;
            var entryAgeInMonths = item.Age;

            var inReferenceRange = GetRangeKey(entryAgeInMonths) == babyRangeKey;

            if (entry.Weight > 0 && GetWeightRangeKey(entryAgeInMonths) == babyWeightKey)
            {
                viewModel.WeightPoints.Add(new Point(entryAgeInMonths, entry.Weight));
            }

            if (entry.Length > 0 && inReferenceRange)
            {
                viewModel.LengthPoints.Add(new Point(entryAgeInMonths, entry.Length));
            }

            if (entry.HeadSize > 0 && entryAgeInMonths < 61)
            {
                viewModel.HeadSizePoints.Add(new Point(entryAgeInMonths, entry.HeadSize));
            }

            if (entry.Length > 0 && entry.Weight > 0 && inReferenceRange)
            {
                var lengthInMeters = entry.Length / 100;
                var bmi = entry.Weight / (lengthInMeters * lengthInMeters);
                viewModel.BMIPoints.Add(new Point(entryAgeInMonths, bmi));
            }
        }

        SetWeightPoints(viewModel, baby, maxAge);
        SetLengthPoints(viewModel, baby, maxAge);
        SetHeadSizePoints(viewModel, baby, maxAge);
        SetBMIPoints(viewModel, baby, maxAge);

        return viewModel;
    }

    private static void SetWeightPoints(ChartsViewModel model, Baby baby, int? maxAge)
    {
        // Without an explicit maxAge, show the WHO reference curve up to 1 year beyond the baby's current age
        var maxAgeInMonthsCeiling = maxAge ?? (int)Math.Ceiling(baby.AgeInMonths() + 12.0);

        var ageBucket = GetWeightRangeKey(baby.AgeInMonths());

        var weightForAgeData = ReadExcel($"wfa_{(baby.Gender == 0 ? "girls" : "boys")}_p_{ageBucket}");
        var data = weightForAgeData.Where(entry => entry.Month <= maxAgeInMonthsCeiling);

        model.WeightPointsSD0 = JsonSerializer.Serialize(data.Select(row => new Point(row.Month, row.P50)).ToList());
        model.WeightPointsSD2 = JsonSerializer.Serialize(data.Select(row => new Point(row.Month, row.P97)).ToList());
        model.WeightPointsSD2neg = JsonSerializer.Serialize(data.Select(row => new Point(row.Month, row.P3)).ToList());
    }

    private static void SetLengthPoints(ChartsViewModel model, Baby baby, int? maxAge)
    {
        // Without an explicit maxAge, show the WHO reference curve up to 1 year beyond the baby's current age
        var maxAgeInMonthsCeiling = maxAge ?? (int)Math.Ceiling(baby.AgeInMonths() + 12.0);

        var ageBucket = GetRangeKey(baby.AgeInMonths());

        var lengthForAgeData = ReadExcel($"lhfa_{(baby.Gender == 0 ? "girls" : "boys")}_p_{ageBucket}");
        var data = lengthForAgeData.Where(entry => entry.Month <= maxAgeInMonthsCeiling);

        model.LengthPointsSD0 = JsonSerializer.Serialize(data.Select(row => new Point(row.Month, row.P50)).ToList());
        model.LengthPointsSD2 = JsonSerializer.Serialize(data.Select(row => new Point(row.Month, row.P97)).ToList());
        model.LengthPointsSD2neg = JsonSerializer.Serialize(data.Select(row => new Point(row.Month, row.P3)).ToList());
    }

    private static void SetHeadSizePoints(ChartsViewModel model, Baby baby, int? maxAge)
    {
        var maxAgeInMonthsCeiling = maxAge
            ?? (int)Math.Ceiling(model.HeadSizePoints.Select(point => point.X).DefaultIfEmpty(0).Max()) + 1;

        var ageBucket = "0_5";

        var headSizeForAgeData = ReadExcel($"hcfa_{(baby.Gender == 0 ? "girls" : "boys")}_p_{ageBucket}");
        var data = headSizeForAgeData.Where(entry => entry.Month <= maxAgeInMonthsCeiling);

        model.HeadSizePointsSD0 = JsonSerializer.Serialize(data.Select(row => new Point(row.Month, row.P50)).ToList());
        model.HeadSizePointsSD2 = JsonSerializer.Serialize(data.Select(row => new Point(row.Month, row.P97)).ToList());
        model.HeadSizePointsSD2neg = JsonSerializer.Serialize(data.Select(row => new Point(row.Month, row.P3)).ToList());
    }

    private static void SetBMIPoints(ChartsViewModel model, Baby baby, int? maxAge)
    {
        // Without an explicit maxAge, show the WHO reference curve up to 1 year beyond the baby's current age
        var maxAgeInMonthsCeiling = maxAge ?? (int)Math.Ceiling(baby.AgeInMonths() + 12.0);

        var ageBucket = GetRangeKey(baby.AgeInMonths());

        var bmiForAgeData = ReadExcel($"bmi_{(baby.Gender == 0 ? "girls" : "boys")}_p_{ageBucket}");
        var data = bmiForAgeData.Where(entry => entry.Month <= maxAgeInMonthsCeiling);

        model.BMIPointsSD0 = JsonSerializer.Serialize(data.Select(row => new Point(row.Month, row.P50)).ToList());
        model.BMIPointsSD2 = JsonSerializer.Serialize(data.Select(row => new Point(row.Month, row.P97)).ToList());
        model.BMIPointsSD2neg = JsonSerializer.Serialize(data.Select(row => new Point(row.Month, row.P3)).ToList());
    }

    private static string GetRangeKey(double ageInMonths)
        => ageInMonths switch
        {
            < 24 => "0_2",
            < 61 => "2_5",
            _ => "5_19"
        };

    private static string GetWeightRangeKey(double ageInMonths)
        => ageInMonths < 61 ? "0_5" : "5_10";

    private static List<ExcelRow> ReadExcel(string fileName)
        => ReferenceCache.GetOrAdd(fileName, name =>
        {
            using var workbook = new XLWorkbook(Path.Combine(AppContext.BaseDirectory, "Charts", $"tab_{name}.xlsx"));

            var range = workbook.Worksheet(1).RangeUsed()
                ?? throw new InvalidOperationException($"Reference file tab_{name}.xlsx has no data");

            return [.. range
                .RowsUsed()
                .Skip(1) // Skip header row
                .Select(row => new ExcelRow
                {
                    Month = row.Cell(1).GetValue<int>(),
                    P3 = row.Cell(8).GetDouble(),
                    P50 = row.Cell(13).GetDouble(),
                    P97 = row.Cell(18).GetDouble()
                })];
        });
}
