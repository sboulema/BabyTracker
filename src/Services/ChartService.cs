using System;
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

namespace BabyTracker.Services;

public interface IChartService
{
    Task<ChartsViewModel> GetViewModel(ClaimsPrincipal user, string babyName, int? maxAge = null);
}

public class ChartService(ISqLiteService sqLiteService) : IChartService
{
    public async Task<ChartsViewModel> GetViewModel(ClaimsPrincipal user, string babyName, int? maxAge = null)
    {
        var viewModel = new ChartsViewModel();

        sqLiteService.OpenDataConnection(user);

        var entries = await sqLiteService.GetGrowth(long.MinValue, long.MaxValue, babyName);

        var babies = await sqLiteService.GetBabiesFromDb();
        var baby = babies.FirstOrDefault(baby => baby.Name == babyName);

        await sqLiteService.CloseDataConnection();

        if (baby == null)
        {
            return viewModel;
        }

        foreach (var entry in entries.Take(maxAge ?? int.MaxValue))
        {
            var entryAgeInMonths = (entry.Time.ToDateTimeUTC() - baby.DOB.ToDateTimeUTC()).Days / (double)30;

            if (entry.Weight > 0)
            {
                viewModel.WeightPoints.Add(new Point(entryAgeInMonths, entry.Weight));
            }

            if (entry.Length > 0 && 
               (baby.AgeInMonths() >= 24 && entryAgeInMonths >= 24 ||
                baby.AgeInMonths() < 24 && entryAgeInMonths < 24))
            {
                viewModel.LengthPoints.Add(new Point(entryAgeInMonths, entry.Length));
            }

            if (entry.HeadSize > 0)
            {
                viewModel.HeadSizePoints.Add(new Point(entryAgeInMonths, entry.HeadSize));
            }

            if (entry.Length > 0 && entry.Weight > 0 && 
               (baby.AgeInMonths() >= 24 && entryAgeInMonths >= 24 ||
                baby.AgeInMonths() < 24 && entryAgeInMonths < 24))
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
        var maxAgeInMonthsCeiling = maxAge ?? (int)Math.Ceiling(model.WeightPoints.Max(p => p.X)) + 1;

        var age = baby.AgeInMonths() switch
        {
            <= 3 => "0_13",
            > 3 and <= 60 => "0_5",
            _ => "0_5"
        };

        var weightForAgeData = ReadExcel($"wfa_{(baby.Gender == 0 ? "girls" : "boys")}_p_{age}");
        var data = weightForAgeData.Take(maxAgeInMonthsCeiling);

        model.WeightPointsSD0 = JsonSerializer.Serialize(data.Select(row => new Point(row.Month, row.P50)).ToList());
        model.WeightPointsSD2 = JsonSerializer.Serialize(data.Select(row => new Point(row.Month, row.P97)).ToList());
        model.WeightPointsSD2neg = JsonSerializer.Serialize(data.Select(row => new Point(row.Month, row.P3)).ToList());
    }

    private static void SetLengthPoints(ChartsViewModel model, Baby baby, int? maxAge)
    {
        var maxAgeInMonthsCeiling = maxAge ?? (int)Math.Ceiling(model.LengthPoints.Max(p => p.X)) + 1;

        var age = baby.AgeInMonths() switch
        {
            <= 3 => "0_13",
            > 3 and <= 24 => "0_2",
            > 24 and <= 60 => "2_5",
            _ => "0_2"
        };

        var lengthForAgeData = ReadExcel($"lhfa_{(baby.Gender == 0 ? "girls" : "boys")}_p_{age}");
        var data = lengthForAgeData.Where(entry => entry.Month <= maxAgeInMonthsCeiling);

        model.LengthPointsSD0 = JsonSerializer.Serialize(data.Select(row => new Point(row.Month, row.P50)).ToList());
        model.LengthPointsSD2 = JsonSerializer.Serialize(data.Select(row => new Point(row.Month, row.P97)).ToList());
        model.LengthPointsSD2neg = JsonSerializer.Serialize(data.Select(row => new Point(row.Month, row.P3)).ToList());
    }

    private static void SetHeadSizePoints(ChartsViewModel model, Baby baby, int? maxAge)
    {
        var maxAgeInMonthsCeiling = maxAge ?? (int)Math.Ceiling(model.HeadSizePoints.Max(p => p.X)) + 1;

        var age = baby.AgeInMonths() switch
        {
            <= 3 => "0_13",
            > 3 and <= 60 => "0_5",
            _ => "0_5"
        };

        var headSizeForAgeData = ReadExcel($"hcfa_{(baby.Gender == 0 ? "girls" : "boys")}_p_{age}");
        var data = headSizeForAgeData.Take(maxAgeInMonthsCeiling);

        model.HeadSizePointsSD0 = JsonSerializer.Serialize(data.Select(row => new Point(row.Month, row.P50)).ToList());
        model.HeadSizePointsSD2 = JsonSerializer.Serialize(data.Select(row => new Point(row.Month, row.P97)).ToList());
        model.HeadSizePointsSD2neg = JsonSerializer.Serialize(data.Select(row => new Point(row.Month, row.P3)).ToList());
    }

    private static void SetBMIPoints(ChartsViewModel model, Baby baby, int? maxAge)
    {
        var maxAgeInMonthsCeiling = maxAge ?? (int)Math.Ceiling(model.BMIPoints.Max(p => p.X)) + 1;

        var age = baby.AgeInMonths() switch
        {
            <= 3 => "0_13",
            > 3 and <= 24 => "0_2",
            > 24 and <= 60 => "2_5",
            _ => "0_2"
        };

        var bmiForAgeData = ReadExcel($"bmi_{(baby.Gender == 0 ? "girls" : "boys")}_p_{age}");
        var data = bmiForAgeData.Where(entry => entry.Month <= maxAgeInMonthsCeiling);

        model.BMIPointsSD0 = JsonSerializer.Serialize(data.Select(row => new Point(row.Month, row.P50)).ToList());
        model.BMIPointsSD2 = JsonSerializer.Serialize(data.Select(row => new Point(row.Month, row.P97)).ToList());
        model.BMIPointsSD2neg = JsonSerializer.Serialize(data.Select(row => new Point(row.Month, row.P3)).ToList());
    }

    private static List<ExcelRow> ReadExcel(string fileName) {
        var rows = new XLWorkbook(Path.Combine("Charts", $"tab_{fileName}.xlsx")).Worksheet(1).RangeUsed().RowsUsed().Skip(1); // Skip header row

        var results = new List<ExcelRow>();

        foreach (var row in rows)
        {
            results.Add(new() {
                Month = row.Cell(1).GetValue<int>(),
                P3 = row.Cell(8).GetDouble(),
                P50 = row.Cell(13).GetDouble(),
                P97 = row.Cell(18).GetDouble()
            }); 
        }

        return results;
    }
}
