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

            entries.AddRange(GetDiapers(path));
            entries.AddRange(GetJoy(path));
            entries.AddRange(GetActivity(path));
            entries.AddRange(GetMilestone(path));

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
                    Time = reader.GetDateTime(0),
                    Note = reader.GetString(1),
                    Status = reader.GetString(2)
                });
            }

            return entries;
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
                    Time = reader.GetDateTime(0),
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
                    Time = reader.GetDateTime(0),
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
                    Time = reader.GetDateTime(0),
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
