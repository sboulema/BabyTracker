using BabyTracker.Models;
using Microsoft.Data.Sqlite;
using System;
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

            return entries;
        }

        public static List<EntryModel> GetDiapers(string path)
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
    }
}
