using BabyTracker.Constants;
using BabyTracker.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;

namespace BabyTracker.Services
{
    public interface ISqLiteService
    {
        void OpenConnection(string path);

        List<EntryModel> GetEntriesFromDb(DateTime date);
    }

    public class SqLiteService : ISqLiteService
    {
        private SqliteConnection _sqliteConnection;

        public void OpenConnection(string path)
        {
            _sqliteConnection = new SqliteConnection($"Data Source={Path.Combine(path, "EasyLog.db")}");
            _sqliteConnection.Open();
        }

        public List<EntryModel> GetEntriesFromDb(DateTime date)
        {
            var lowerBound = ToUnixTimestamp(date);
            var upperBound = ToUnixTimestamp(date.AddDays(1));

            var entries = new List<EntryModel>();

            entries.AddRange(GetActivity(lowerBound, upperBound));
            entries.AddRange(GetDiapers(lowerBound, upperBound));
            entries.AddRange(GetFormula(lowerBound, upperBound));
            entries.AddRange(GetGrowth(lowerBound, upperBound));
            entries.AddRange(GetJoy(lowerBound, upperBound));
            entries.AddRange(GetMedication(lowerBound, upperBound));
            entries.AddRange(GetMilestone(lowerBound, upperBound));
            entries.AddRange(GetSleep(lowerBound, upperBound));
            entries.AddRange(GetSupplement(lowerBound, upperBound));
            entries.AddRange(GetTemperature(lowerBound, upperBound));
            entries.AddRange(GetVaccine(lowerBound, upperBound));

            return entries;
        }

        private List<EntryModel> GetSupplement(long lowerBound, long upperBound)
        {
            var entries = new List<EntryModel>();

            var command = _sqliteConnection.CreateCommand();
            command.CommandText = "SELECT dateTime(Time, 'unixepoch'), Note, Amount, Unit, Name, Description" +
            " FROM OtherFeed LEFT JOIN OtherFeedSelection ON TypeID == OtherFeedSelection.ID" +
            $" WHERE Time >= {lowerBound} AND Time <= {upperBound}";

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

        private List<EntryModel> GetMedication(long lowerBound, long upperBound)
        {
            var entries = new List<EntryModel>();

            var command = _sqliteConnection.CreateCommand();
            command.CommandText = "SELECT dateTime(Time, 'unixepoch'), Note, Name, Description, Amount, AmountPerTime, Unit" +
            " FROM Medicine LEFT JOIN MedicineSelection ON MedID == MedicineSelection.ID" +
            $" WHERE Time >= {lowerBound} AND Time <= {upperBound}";

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

        private List<EntryModel> GetVaccine(long lowerBound, long upperBound)
        {
            var entries = new List<EntryModel>();

            var command = _sqliteConnection.CreateCommand();
            command.CommandText = "SELECT dateTime(Time, 'unixepoch'), Note, Name, Description" +
            " FROM Vaccine LEFT JOIN VaccineSelection ON VaccID == VaccineSelection.ID" +
            $" WHERE Time >= {lowerBound} AND Time <= {upperBound}";

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

        private List<EntryModel> GetTemperature(long lowerBound, long upperBound)
        {
            var entries = new List<EntryModel>();

            var command = _sqliteConnection.CreateCommand();
            command.CommandText = "SELECT dateTime(Time, 'unixepoch'), Note, Temp" +
            " FROM Temperature" +
            $" WHERE Time >= {lowerBound} AND Time <= {upperBound}";

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

        private List<EntryModel> GetSleep(long lowerBound, long upperBound)
        {
            var entries = new List<EntryModel>();

            var command = _sqliteConnection.CreateCommand();
            command.CommandText = "SELECT dateTime(Time, 'unixepoch'), Note, Duration" +
            " FROM Sleep" +
            $" WHERE Time >= {lowerBound} AND Time <= {upperBound}";

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

        private List<EntryModel> GetGrowth(long lowerBound, long upperBound)
        {
            var entries = new List<EntryModel>();

            var command = _sqliteConnection.CreateCommand();
            command.CommandText = "SELECT dateTime(Time, 'unixepoch'), Note, Weight, Length, Head" +
            " FROM Growth" +
            $" WHERE Time >= {lowerBound} AND Time <= {upperBound}";

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

        private List<EntryModel> GetFormula(long lowerBound, long upperBound)
        {
            var entries = new List<EntryModel>();

            var command = _sqliteConnection.CreateCommand();
            command.CommandText = "SELECT dateTime(Time, 'unixepoch'), Note, Amount" +
            " FROM Formula" +
            $" WHERE Time >= {lowerBound} AND Time <= {upperBound}";

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

        private List<EntryModel> GetDiapers(long lowerBound, long upperBound)
        {
            var entries = new List<EntryModel>();

            var command = _sqliteConnection.CreateCommand();
            command.CommandText = "SELECT dateTime(Time, 'unixepoch'), Note, Status" +
            " FROM Diaper" +
            $" WHERE Time >= {lowerBound} AND Time <= {upperBound}";

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

        private static string GetDiaperStatus(string status)
            => status switch
            {
                DiaperStatus.Wet => "Wet",
                DiaperStatus.Dirty => "Dirty",
                DiaperStatus.Mixed => "Mixed",
                _ => string.Empty,
            };

        private List<EntryModel> GetJoy(long lowerBound, long upperBound)
        {
            var entries = new List<EntryModel>();

            var command = _sqliteConnection.CreateCommand();
            command.CommandText = "SELECT dateTime(Time, 'unixepoch'), Note, Filename" +
            " FROM Joy LEFT JOIN Picture ON Joy.Id == activityid" +
            $" WHERE Time >= {lowerBound} AND Time <= {upperBound}";

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

        private List<EntryModel> GetActivity(long lowerBound, long upperBound)
        {
            var entries = new List<EntryModel>();

            var command = _sqliteConnection.CreateCommand();
            command.CommandText = "SELECT dateTime(Time, 'unixepoch'), Note, Filename, Duration, Name " +
                "FROM OtherActivity " +
                "LEFT JOIN Picture ON OtherActivity.Id == activityid " +
                "LEFT JOIN OtherActivityDesc ON OtherActivityDesc.Id == DescID" +
            $" WHERE Time >= {lowerBound} AND Time <= {upperBound}";

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

        private List<EntryModel> GetMilestone(long lowerBound, long upperBound)
        {
            var entries = new List<EntryModel>();

            var command = _sqliteConnection.CreateCommand();
            command.CommandText = "SELECT dateTime(Time, 'unixepoch'), Note, Filename, Name " +
                "FROM Milestone " +
                "LEFT JOIN Picture ON Milestone.Id == activityid " +
                "LEFT JOIN MilestoneSelection ON MilestoneSelection.Id == Milestoneselectionid" +
            $" WHERE Time >= {lowerBound} AND Time <= {upperBound}";

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

        private long ToUnixTimestamp(DateTime date) => new DateTimeOffset(date).ToUnixTimeSeconds();
    }
}
