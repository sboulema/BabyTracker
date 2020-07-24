using BabyTracker.Constants;
using BabyTracker.Models;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.IO;

namespace BabyTracker.Services
{
    public static class SqLiteService
    {
        public static List<EntryModel> ParseDb(string path)
        {
            var entries = new List<EntryModel>();

            entries.AddRange(GetActivity(path));
            entries.AddRange(GetDiapers(path));
            entries.AddRange(GetFormula(path));
            entries.AddRange(GetGrowth(path));
            entries.AddRange(GetJoy(path));
            entries.AddRange(GetMedication(path));
            entries.AddRange(GetMilestone(path));
            entries.AddRange(GetSleep(path));
            entries.AddRange(GetSupplement(path));
            entries.AddRange(GetTemperature(path));
            entries.AddRange(GetVaccine(path));

            return entries;
        }

        private static List<EntryModel> GetSupplement(string path)
        {
            var entries = new List<EntryModel>();

            using var connection = new SqliteConnection($"Data Source={Path.Combine(path, "EasyLog.db")}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT dateTime(Time, 'unixepoch'), Note, Amount, Unit, Name, Description" + 
            " FROM OtherFeed LEFT JOIN OtherFeedSelection ON TypeID == OtherFeedSelection.ID";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                entries.Add(new SupplementModel
                {
                    TimeUTC = reader.GetDateTime(0),
                    Note = reader.GetString(1),
                    Supplement = $"{reader.GetString(4)} {reader.GetString(2)} {reader.GetString(3)}"
                });
            }

            return entries;
        }

        private static List<EntryModel> GetMedication(string path)
        {
            var entries = new List<EntryModel>();

            using var connection = new SqliteConnection($"Data Source={Path.Combine(path, "EasyLog.db")}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT dateTime(Time, 'unixepoch'), Note, Name, Description, Amount, AmountPerTime, Unit" + 
            " FROM Medicine LEFT JOIN MedicineSelection ON MedID == MedicineSelection.ID";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                entries.Add(new MedicationModel
                {
                    TimeUTC = reader.GetDateTime(0),
                    Note = reader.GetString(1),
                    MedicationName = $"{reader.GetString(3)}",
                    Amount = reader.GetString(4)
                });
            }

            return entries;
        }

        private static List<EntryModel> GetVaccine(string path)
        {
            var entries = new List<EntryModel>();

            using var connection = new SqliteConnection($"Data Source={Path.Combine(path, "EasyLog.db")}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT dateTime(Time, 'unixepoch'), Note, Name, Description FROM Vaccine LEFT JOIN VaccineSelection ON VaccID == VaccineSelection.ID";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                entries.Add(new VaccineModel
                {
                    TimeUTC = reader.GetDateTime(0),
                    Note = reader.GetString(1),
                    Vaccine = $"{reader.GetString(2)} - {reader.GetString(3)}"
                });
            }

            return entries;
        }

        private static List<EntryModel> GetTemperature(string path)
        {
            var entries = new List<EntryModel>();

            using var connection = new SqliteConnection($"Data Source={Path.Combine(path, "EasyLog.db")}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT dateTime(Time, 'unixepoch'), Note, Temp FROM Temperature";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                entries.Add(new TemperatureModel
                {
                    TimeUTC = reader.GetDateTime(0),
                    Note = reader.GetString(1),
                    Temperature = reader.GetDouble(2)
                });
            }

            return entries;
        }

        private static List<EntryModel> GetSleep(string path)
        {
            var entries = new List<EntryModel>();

            using var connection = new SqliteConnection($"Data Source={Path.Combine(path, "EasyLog.db")}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT dateTime(Time, 'unixepoch'), Note, Duration FROM Sleep";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                entries.Add(new SleepModel
                {
                    TimeUTC = reader.GetDateTime(0),
                    Note = reader.GetString(1),
                    Duration = reader.GetInt32(2).ToString()
                });
            }

            return entries;
        }

        private static List<EntryModel> GetGrowth(string path)
        {
            var entries = new List<EntryModel>();

            using var connection = new SqliteConnection($"Data Source={Path.Combine(path, "EasyLog.db")}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT dateTime(Time, 'unixepoch'), Note, Weight, Length, Head FROM Growth";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                entries.Add(new Growth
                {
                    TimeUTC = reader.GetDateTime(0),
                    Note = reader.GetString(1),
                    Weight = reader.GetDouble(2),
                    Length = reader.GetDouble(3),
                    HeadSize = reader.GetDouble(4)
                });
            }

            return entries;
        }

        private static List<EntryModel> GetFormula(string path)
        {
            var entries = new List<EntryModel>();

            using var connection = new SqliteConnection($"Data Source={Path.Combine(path, "EasyLog.db")}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT dateTime(Time, 'unixepoch'), Note, Amount FROM Formula";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                entries.Add(new Formula
                {
                    TimeUTC = reader.GetDateTime(0),
                    Note = reader.GetString(1),
                    Amount = reader.GetInt32(2).ToString()
                });
            }

            return entries;
        }

        private static List<EntryModel> GetDiapers(string path)
        {
            var entries = new List<EntryModel>();

            using var connection = new SqliteConnection($"Data Source={Path.Combine(path, "EasyLog.db")}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT dateTime(Time, 'unixepoch'), Note, Status FROM Diaper";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                entries.Add(new Diaper
                {
                    TimeUTC = reader.GetDateTime(0),
                    Note = reader.GetString(1),
                    Status = GetDiaperStatus(reader.GetString(2))
                });
            }

            return entries;
        }

        private static string GetDiaperStatus(string status) {
            switch (status)
            {
                case DiaperStatus.Wet:
                    return "Wet";
                case DiaperStatus.Dirty:
                    return "Dirty";
                case DiaperStatus.Mixed:
                    return "Mixed";
                default:
                    return string.Empty;
            }
        }

        private static List<EntryModel> GetJoy(string path)
        {
            var entries = new List<EntryModel>();

            using var connection = new SqliteConnection($"Data Source={Path.Combine(path, "EasyLog.db")}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT dateTime(Time, 'unixepoch'), Note, Filename FROM Joy LEFT JOIN Picture ON Joy.Id == activityid";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                entries.Add(new Joy
                {
                    TimeUTC = reader.GetDateTime(0),
                    Note = reader.GetString(1),
                    Filename = reader.GetString(2)
                });
            }

            return entries;
        }

        private static List<EntryModel> GetActivity(string path)
        {
            var entries = new List<EntryModel>();

            using var connection = new SqliteConnection($"Data Source={Path.Combine(path, "EasyLog.db")}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT dateTime(Time, 'unixepoch'), Note, Filename, Duration, Name " + 
                "FROM OtherActivity " + 
                "LEFT JOIN Picture ON OtherActivity.Id == activityid " +
                "LEFT JOIN OtherActivityDesc ON OtherActivityDesc.Id == DescID";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                entries.Add(new ActivityModel
                {
                    TimeUTC = reader.GetDateTime(0),
                    Note = reader.GetString(1),
                    Filename = GetString(reader, 2),
                    Duration = reader.GetInt32(3).ToString(),
                    OtherActivity = GetString(reader, 4)
                });
            }

            return entries;
        }

        private static List<EntryModel> GetMilestone(string path)
        {
            var entries = new List<EntryModel>();

            using var connection = new SqliteConnection($"Data Source={Path.Combine(path, "EasyLog.db")}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT dateTime(Time, 'unixepoch'), Note, Filename, Name " +
                "FROM Milestone " +
                "LEFT JOIN Picture ON Milestone.Id == activityid " +
                "LEFT JOIN MilestoneSelection ON MilestoneSelection.Id == Milestoneselectionid";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                entries.Add(new MilestoneModel
                {
                    TimeUTC = reader.GetDateTime(0),
                    Note = reader.GetString(1),
                    Filename = GetString(reader, 2),
                    Milestone = GetString(reader, 3)
                });
            }

            return entries;
        }

        private static string GetString(SqliteDataReader reader, int column) => reader.IsDBNull(column) ? string.Empty : reader.GetString(column);
    }
}
