using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using BabyTracker.Models;
using BabyTracker.Models.Charts;
using BabyTracker.Models.ViewModels;
using CsvHelper;

namespace BabyTracker.Services
{
    public interface IChartService 
    {
        ChartsViewModel GetViewModel(string babyName);
    }

    public class ChartService : IChartService
    {
        private readonly ISqLiteService _sqLiteService;
        private readonly IEnumerable<CsvRow> _weightForAgeData;
        private readonly IEnumerable<CsvRow> _lengthForAgeData;
        private readonly IEnumerable<CsvRow> _headSizeForAgeData;
        private readonly IEnumerable<CsvRow> _bmiForAgeData;

        public ChartService(ISqLiteService sqLiteService)
        {
            _sqLiteService = sqLiteService;
            _weightForAgeData = ReadCsv("wfa-g-z");
            _lengthForAgeData = ReadCsv("lfa-g-z");
            _headSizeForAgeData = ReadCsv("hfa-g-z");
            _bmiForAgeData = ReadCsv("bfa-g-z");
        }

        public ChartsViewModel GetViewModel(string babyName)
        {
            var result = new ChartsViewModel();

            var connection = _sqLiteService.OpenConnection(babyName);
            var entries = _sqLiteService.GetGrowth(long.MinValue, long.MaxValue, babyName, connection);
            var baby = _sqLiteService.GetBaby(babyName, connection).FirstOrDefault() as BabyModel;

            connection.Close();

            foreach (Growth entry in entries)
            {
                var ageInMonths = (entry.TimeUTC - baby.DateOfBirth).Days / (double)30;

                if (entry.Weight > 0)
                {
                    result.WeightPoints.Add(new Point(ageInMonths, entry.Weight));
                }

                if (entry.Length > 0)
                {
                    result.LengthPoints.Add(new Point(ageInMonths, entry.Length));
                }

                if (entry.HeadSize > 0)
                {
                    result.HeadSizePoints.Add(new Point(ageInMonths, entry.HeadSize));
                }

                if (entry.Length > 0 && entry.Weight > 0) 
                {
                    var lengthInMeters = entry.Length / 100;
                    var bmi = entry.Weight / (lengthInMeters * lengthInMeters);
                    result.BMIPoints.Add(new Point(ageInMonths, bmi));
                }
            }

            SetWeightCsvPoints(result);
            SetLengthCsvPoints(result);
            SetHeadSizeCsvPoints(result);
            SetBMICsvPoints(result);

            return result;
        }

        private List<Point> GetWeightMedian() => _weightForAgeData.Select(row => new Point(row.Month, row.SD0)).ToList();

        private void SetWeightCsvPoints(ChartsViewModel model) 
        {
            var maxAgeInMonthsCeiling = (int)Math.Ceiling(model.WeightPoints.Max(p => p.X)) + 1;
            var data = _weightForAgeData.Take(maxAgeInMonthsCeiling);
            model.WeightPointsSD0 = JsonSerializer.Serialize(data.Select(row => new Point(row.Month, row.SD0)).ToList());
            model.WeightPointsSD2 = JsonSerializer.Serialize(data.Select(row => new Point(row.Month, row.SD2)).ToList());
            model.WeightPointsSD2neg = JsonSerializer.Serialize(data.Select(row => new Point(row.Month, row.SD2neg)).ToList());
        }

        private void SetLengthCsvPoints(ChartsViewModel model) 
        {
            var maxAgeInMonthsCeiling = (int)Math.Ceiling(model.LengthPoints.Max(p => p.X)) + 1;
            var data = _lengthForAgeData.Take(maxAgeInMonthsCeiling);
            model.LengthPointsSD0 = JsonSerializer.Serialize(data.Select(row => new Point(row.Month, row.SD0)).ToList());
            model.LengthPointsSD2 = JsonSerializer.Serialize(data.Select(row => new Point(row.Month, row.SD2)).ToList());
            model.LengthPointsSD2neg = JsonSerializer.Serialize(data.Select(row => new Point(row.Month, row.SD2neg)).ToList());
        }

        private void SetHeadSizeCsvPoints(ChartsViewModel model) 
        {
            var maxAgeInMonthsCeiling = (int)Math.Ceiling(model.HeadSizePoints.Max(p => p.X)) + 1;
            var data = _headSizeForAgeData.Take(maxAgeInMonthsCeiling);
            model.HeadSizePointsSD0 = JsonSerializer.Serialize(data.Select(row => new Point(row.Month, row.SD0)).ToList());
            model.HeadSizePointsSD2 = JsonSerializer.Serialize(data.Select(row => new Point(row.Month, row.SD2)).ToList());
            model.HeadSizePointsSD2neg = JsonSerializer.Serialize(data.Select(row => new Point(row.Month, row.SD2neg)).ToList());
        }

        private void SetBMICsvPoints(ChartsViewModel model) 
        {
            var maxAgeInMonthsCeiling = (int)Math.Ceiling(model.BMIPoints.Max(p => p.X)) + 1;
            var data = _bmiForAgeData.Take(maxAgeInMonthsCeiling);
            model.BMIPointsSD0 = JsonSerializer.Serialize(data.Select(row => new Point(row.Month, row.SD0)).ToList());
            model.BMIPointsSD2 = JsonSerializer.Serialize(data.Select(row => new Point(row.Month, row.SD2)).ToList());
            model.BMIPointsSD2neg = JsonSerializer.Serialize(data.Select(row => new Point(row.Month, row.SD2neg)).ToList());
        }

        private IEnumerable<CsvRow> ReadCsv(string fileName)
        {
            using var reader = new StreamReader(Path.Combine("Charts", $"{fileName}.csv"));
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            var records = csv.GetRecords<CsvRow>().ToList();
            return records;
        }
    }
}