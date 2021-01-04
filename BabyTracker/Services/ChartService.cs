using System;
using System.Collections.Generic;
using System.Linq;
using BabyTracker.Models;
using BabyTracker.Models.Charts;
using BabyTracker.Models.ViewModels;

namespace BabyTracker.Services
{
    public interface IChartService 
    {
        ChartsViewModel GetViewModel(string babyName);
    }

    public class ChartService : IChartService
    {
        private readonly ISqLiteService _sqLiteService;

        public ChartService(ISqLiteService sqLiteService)
        {
            _sqLiteService = sqLiteService;
        }

        public ChartsViewModel GetViewModel(string babyName)
        {
            var result = new ChartsViewModel();

            var connection = _sqLiteService.OpenConnection($"/data/{babyName}");
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

            var MaxAgeInMonthsCeiling = (int)Math.Ceiling(result.WeightPoints.Max(p => p.X));

            result.WeightPointsMedian = GetWeightMedian().Take(MaxAgeInMonthsCeiling + 1).ToList();

            return result;
        }

        private List<Point> GetWeightMedian() 
        {
            return new List<Point> {
                new Point(0, 3.2),
                new Point(1, 4.2),
                new Point(2, 5.1),
                new Point(3, 5.8),
                new Point(4, 6.4),
                new Point(5, 6.9),
                new Point(6, 7.3),
                new Point(7, 7.6),
                new Point(8, 7.9),
                new Point(9, 8.2),
                new Point(10, 8.5),
                new Point(11, 8.7),
                new Point(12, 8.9),
                new Point(13, 9.2),
                new Point(14, 9.4),
                new Point(15, 9.6),
                new Point(16, 9.8),
                new Point(17, 10),
                new Point(18, 10.2),
                new Point(19, 10.4),
                new Point(20, 10.6),
                new Point(21, 10.9),
                new Point(22, 11.1),
                new Point(23, 11.3),
                new Point(24, 11.5),
                new Point(25, 11.7),
                new Point(26, 11.9),
                new Point(27, 12.1),
                new Point(28, 12.3),
                new Point(29, 12.5),
                new Point(30, 12.7),
                new Point(31, 12.9),
                new Point(32, 13.1),
                new Point(33, 13.3),
                new Point(34, 13.5),
                new Point(35, 13.7),
                new Point(36, 13.9),
                new Point(37, 14),
                new Point(38, 14.2),
                new Point(39, 14.4),
                new Point(40, 14.6),
                new Point(41, 14.8),
                new Point(42, 15),
                new Point(43, 15.2),
                new Point(44, 15.3),
                new Point(45, 15.5),
                new Point(46, 15.7),
                new Point(47, 15.9),
                new Point(48, 16.1),
                new Point(49, 16.3),
                new Point(50, 16.4),
                new Point(51, 16.6),
                new Point(52, 16.8),
                new Point(53, 17.0),
                new Point(54, 17.2),
                new Point(55, 17.3),
                new Point(56, 17.5),
                new Point(57, 17.7),
                new Point(58, 17.9),
                new Point(59, 18),
                new Point(60, 18.2),
            };
        }
    }
}