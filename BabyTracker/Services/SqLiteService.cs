using BabyTracker.Constants;
using BabyTracker.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BabyTracker.Services;

public interface ISqLiteService
{
    Task<SqliteConnection> OpenConnection(ClaimsPrincipal user);

    Task<List<EntryModel>> GetEntriesFromDb(DateTime date, ClaimsPrincipal user, string babyName);

    Task<List<EntryModel>> GetMemoriesFromDb(DateTime date, ClaimsPrincipal user, string babyName);

    List<EntryModel> GetMemoriesFromDb(DateTime date, string userId, string babyName);

    List<EntryModel> GetGrowth(long lowerBound, long upperBound, string babyName, SqliteConnection connection);

    Task<List<EntryModel>> GetBabiesFromDb(ClaimsPrincipal user);

    List<EntryModel> GetBabiesFromDb(string userId);

    Task<DateTime> GetLastEntryDateTime(ClaimsPrincipal user, string babyName);

    Task<List<DateOnly>> GetAllEntryDates(ClaimsPrincipal user, string babyName);

    Task<List<PictureModel>> GetPictures(ClaimsPrincipal user, string babyName);
}

public class SqLiteService : ISqLiteService
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IAccountService _accountService;

    public SqLiteService(IWebHostEnvironment webHostEnvironment,
        IAccountService accountService)
    {
        _webHostEnvironment = webHostEnvironment;
        _accountService = accountService;
    }

    public async Task<SqliteConnection> OpenConnection(ClaimsPrincipal user)
    {
        var profile = await _accountService.GetProfile(user);

        return OpenConnection(profile?.UserId);
    }

    /// <summary>
    /// Open a connection with the sqlite db file
    /// </summary>
    /// <param name="userId">userId without prefix</param>
    /// <returns></returns>
    public SqliteConnection OpenConnection(string userId)
    {
        var path = $"/data/Data/{userId}/EasyLog.db";

        if (!_webHostEnvironment.IsProduction())
        {
            path = Path.Combine(_webHostEnvironment.ContentRootPath, "Data", userId, "EasyLog.db");
        }

        var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();
        return connection;
    }

    public async Task<List<EntryModel>> GetEntriesFromDb(DateTime date, ClaimsPrincipal user, string babyName)
    {
        var connection = await OpenConnection(user);

        var lowerBound = ToUnixTimestamp(date);
        var upperBound = ToUnixTimestamp(date.AddDays(1));

        var entries = new List<EntryModel>();

        entries.AddRange(GetActivity(lowerBound, upperBound, babyName, connection));
        entries.AddRange(GetDiapers(lowerBound, upperBound, babyName, connection));
        entries.AddRange(GetFormula(lowerBound, upperBound, babyName, connection));
        entries.AddRange(GetGrowth(lowerBound, upperBound, babyName, connection));
        entries.AddRange(GetJoy(lowerBound, upperBound, babyName, connection));
        entries.AddRange(GetMedication(lowerBound, upperBound, babyName, connection));
        entries.AddRange(GetMilestone(lowerBound, upperBound, babyName, connection));
        entries.AddRange(GetSleep(lowerBound, upperBound, babyName, connection));
        entries.AddRange(GetSupplement(lowerBound, upperBound, babyName, connection));
        entries.AddRange(GetTemperature(lowerBound, upperBound, babyName, connection));
        entries.AddRange(GetVaccine(lowerBound, upperBound, babyName, connection));

        connection.Close();

        return entries;
    }

    public async Task<List<EntryModel>> GetMemoriesFromDb(DateTime date, ClaimsPrincipal user, string babyName)
    {
        var connection = await OpenConnection(user);

        var entries = new List<EntryModel>();

        entries.AddRange(GetActivity(date.Day, date.Month, babyName, connection));
        entries.AddRange(GetJoy(date.Day, date.Month, babyName, connection));
        entries.AddRange(GetMilestone(date.Day, date.Month, babyName, connection));

        connection.Close();

        return entries;
    }

    public List<EntryModel> GetMemoriesFromDb(DateTime date, string userId, string babyName)
    {
        var connection = OpenConnection(userId);

        var entries = new List<EntryModel>();

        entries.AddRange(GetActivity(date.Day, date.Month, babyName, connection));
        entries.AddRange(GetJoy(date.Day, date.Month, babyName, connection));
        entries.AddRange(GetMilestone(date.Day, date.Month, babyName, connection));

        connection.Close();

        return entries;
    }

    public async Task<List<EntryModel>> GetBabiesFromDb(ClaimsPrincipal user)
    {
        var profile = await _accountService.GetProfile(user);

        return GetBabiesFromDb(profile?.UserId);
    }

    public List<EntryModel> GetBabiesFromDb(string userId)
    {
        var connection = OpenConnection(userId);

        var entries = new List<EntryModel>();

        var command = connection.CreateCommand();
        command.CommandText =
            "SELECT " +
                "dateTime(Timestamp, 'unixepoch'), " +
                "Name, dateTime(DOB, 'unixepoch'), " +
                "dateTime(DueDay, 'unixepoch'), " +
                "Gender, Picture" +
            " FROM Baby";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            entries.Add(new BabyModel
            {
                TimeUTC = reader.GetDateTime(0),
                BabyName = GetString(reader, 1),
                DateOfBirth = reader.GetDateTime(2),
                DueDate = reader.GetDateTime(3),
                Gender = reader.GetInt32(4),
                PictureFileName = reader.GetString(5)
            });
        }

        connection.Close();

        return entries;
    }

    public async Task<DateTime> GetLastEntryDateTime(ClaimsPrincipal user, string babyName)
    {
        var connection = await OpenConnection(user);

        var command = connection.CreateCommand();
        command.CommandText = "SELECT dateTime(Timestamp, 'unixepoch') AS Timestamp FROM Diaper UNION ALL " +
                              "SELECT dateTime(Timestamp, 'unixepoch') AS Timestamp FROM Formula UNION ALL " +
                              "SELECT dateTime(Timestamp, 'unixepoch') AS Timestamp FROM Growth UNION ALL " +
                              "SELECT dateTime(Timestamp, 'unixepoch') AS Timestamp FROM Joy UNION ALL " +
                              "SELECT dateTime(Timestamp, 'unixepoch') AS Timestamp FROM Medicine UNION ALL " +
                              "SELECT dateTime(Timestamp, 'unixepoch') AS Timestamp FROM Milestone UNION ALL " +
                              "SELECT dateTime(Timestamp, 'unixepoch') AS Timestamp FROM OtherActivity UNION ALL " +
                              "SELECT dateTime(Timestamp, 'unixepoch') AS Timestamp FROM Sleep UNION ALL " +
                              "SELECT dateTime(Timestamp, 'unixepoch') AS Timestamp FROM Temperature UNION ALL " +
                              "SELECT dateTime(Timestamp, 'unixepoch') AS Timestamp FROM Vaccine " +
                              "ORDER BY Timestamp DESC " +
                              "LIMIT 1";

        using var reader = command.ExecuteReader();
        reader.Read();

        return reader.GetDateTime(0);
    }

    public async Task<List<DateOnly>> GetAllEntryDates(ClaimsPrincipal user, string babyName)
    {
        var dates = new List<DateOnly>();

        var connection = await OpenConnection(user);

        var command = connection.CreateCommand();
        command.CommandText = """
            SELECT date(Timestamp, 'unixepoch') AS Timestamp FROM Diaper UNION 
            SELECT date(Timestamp, 'unixepoch') AS Timestamp FROM Formula UNION
            SELECT date(Timestamp, 'unixepoch') AS Timestamp FROM Growth UNION
            SELECT date(Timestamp, 'unixepoch') AS Timestamp FROM Joy UNION
            SELECT date(Timestamp, 'unixepoch') AS Timestamp FROM Medicine UNION
            SELECT date(Timestamp, 'unixepoch') AS Timestamp FROM Milestone UNION
            SELECT date(Timestamp, 'unixepoch') AS Timestamp FROM OtherActivity UNION
            SELECT date(Timestamp, 'unixepoch') AS Timestamp FROM Sleep UNION
            SELECT date(Timestamp, 'unixepoch') AS Timestamp FROM Temperature UNION
            SELECT date(Timestamp, 'unixepoch') AS Timestamp FROM Vaccine
            """;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            dates.Add(DateOnly.FromDateTime(reader.GetDateTime(0)));
        }

        return dates;
    }

    public async Task<List<PictureModel>> GetPictures(ClaimsPrincipal user, string babyName)
    {
        var pictures = new List<PictureModel>();

        var connection = await OpenConnection(user);

        var command = connection.CreateCommand();
        command.CommandText = "SELECT dateTime(Timestamp, 'unixepoch'), FileName" +
        " FROM Picture ORDER BY Timestamp DESC";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            pictures.Add(new PictureModel
            {
                TimeUTC = reader.GetDateTime(0),
                Filename = GetString(reader, 1)
            });
        }

        return pictures;
    }

    private static List<EntryModel> GetSupplement(long lowerBound, long upperBound,
        string babyName, SqliteConnection connection)
    {
        var entries = new List<EntryModel>();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT dateTime(Time, 'unixepoch'), Note, Amount, Unit, Name, Description" +
        " FROM OtherFeed LEFT JOIN OtherFeedSelection ON TypeID == OtherFeedSelection.ID" +
        $" WHERE Time >= {lowerBound} AND Time <= {upperBound}";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            entries.Add(new SupplementModel
            {
                TimeUTC = reader.GetDateTime(0),
                Note = GetString(reader, 1),
                Supplement = $"{GetString(reader, 4)} {GetString(reader, 2)} {GetString(reader, 3)}",
                BabyName = babyName
            });
        }

        return entries;
    }

    private static List<EntryModel> GetMedication(long lowerBound, long upperBound,
        string babyName, SqliteConnection connection)
    {
        var entries = new List<EntryModel>();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT dateTime(Time, 'unixepoch'), Note, Name, Description, Amount, AmountPerTime, Unit" +
        " FROM Medicine LEFT JOIN MedicineSelection ON MedID == MedicineSelection.ID" +
        $" WHERE Time >= {lowerBound} AND Time <= {upperBound}";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            entries.Add(new MedicationModel
            {
                BabyName = babyName,
                TimeUTC = reader.GetDateTime(0),
                Note = GetString(reader, 1),
                MedicationName = $"{GetString(reader, 2)}",
                Amount = reader.GetInt32(4),
                AmountPerTime = reader.GetInt32(5),
                Unit = GetString(reader, 6)
            });
        }

        return entries;
    }

    private static List<EntryModel> GetVaccine(long lowerBound, long upperBound,
        string babyName, SqliteConnection connection)
    {
        var entries = new List<EntryModel>();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT dateTime(Time, 'unixepoch'), Note, Name, Description" +
        " FROM Vaccine LEFT JOIN VaccineSelection ON VaccID == VaccineSelection.ID" +
        $" WHERE Time >= {lowerBound} AND Time <= {upperBound}";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            entries.Add(new VaccineModel
            {
                BabyName = babyName,
                TimeUTC = reader.GetDateTime(0),
                Note = GetString(reader, 1),
                Vaccine = $"{GetString(reader, 2)} - {GetString(reader, 3)}"
            });
        }

        return entries;
    }

    private static List<EntryModel> GetTemperature(long lowerBound, long upperBound,
        string babyName, SqliteConnection connection)
    {
        var entries = new List<EntryModel>();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT dateTime(Time, 'unixepoch'), Note, Temp" +
        " FROM Temperature" +
        $" WHERE Time >= {lowerBound} AND Time <= {upperBound}";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            entries.Add(new TemperatureModel
            {
                BabyName = babyName,
                TimeUTC = reader.GetDateTime(0),
                Note = GetString(reader, 1),
                Temperature = reader.GetDouble(2)
            });
        }

        return entries;
    }

    private static List<EntryModel> GetSleep(long lowerBound, long upperBound,
        string babyName, SqliteConnection connection)
    {
        var entries = new List<EntryModel>();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT dateTime(Time, 'unixepoch'), Note, Duration" +
        " FROM Sleep" +
        $" WHERE Time >= {lowerBound} AND Time <= {upperBound}";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            entries.Add(new SleepModel
            {
                BabyName = babyName,
                TimeUTC = reader.GetDateTime(0),
                Note = GetString(reader, 1),
                Duration = reader.GetInt32(2).ToString()
            });
        }

        return entries;
    }

    public List<EntryModel> GetGrowth(long lowerBound, long upperBound,
        string babyName, SqliteConnection connection)
    {
        var entries = new List<EntryModel>();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT dateTime(Time, 'unixepoch'), Note, Weight, Length, Head" +
        " FROM Growth" +
        $" WHERE Time >= {lowerBound} AND Time <= {upperBound}";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            entries.Add(new Growth
            {
                BabyName = babyName,
                TimeUTC = reader.GetDateTime(0),
                Note = GetString(reader, 1),
                Weight = reader.GetDouble(2),
                Length = reader.GetDouble(3),
                HeadSize = reader.GetDouble(4)
            });
        }

        return entries;
    }

    private static List<EntryModel> GetFormula(long lowerBound, long upperBound,
        string babyName, SqliteConnection connection)
    {
        var entries = new List<EntryModel>();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT dateTime(Time, 'unixepoch'), Note, Amount" +
        " FROM Formula" +
        $" WHERE Time >= {lowerBound} AND Time <= {upperBound}";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            entries.Add(new Formula
            {
                BabyName = babyName,
                TimeUTC = reader.GetDateTime(0),
                Note = GetString(reader, 1),
                Amount = reader.GetInt32(2).ToString()
            });
        }

        return entries;
    }

    private static List<EntryModel> GetDiapers(long lowerBound, long upperBound,
        string babyName, SqliteConnection connection)
    {
        var entries = new List<EntryModel>();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT dateTime(Time, 'unixepoch'), Note, Status" +
        " FROM Diaper" +
        $" WHERE Time >= {lowerBound} AND Time <= {upperBound}";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            entries.Add(new Diaper
            {
                BabyName = babyName,
                TimeUTC = reader.GetDateTime(0),
                Note = GetString(reader, 1),
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

    private static List<EntryModel> GetJoy(long lowerBound, long upperBound,
        string babyName, SqliteConnection connection)
    {
        var entries = new List<EntryModel>();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT dateTime(Time, 'unixepoch'), Note, Filename" +
        " FROM Joy LEFT JOIN Picture ON Joy.Id == activityid" +
        $" WHERE Time >= {lowerBound} AND Time <= {upperBound}";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            entries.Add(new Joy
            {
                BabyName = babyName,
                TimeUTC = reader.GetDateTime(0),
                Note = GetString(reader, 1),
                Filename = GetString(reader, 2)
            });
        }

        return entries;
    }

    private static List<EntryModel> GetJoy(int day, int month,
        string babyName, SqliteConnection connection)
    {
        var entries = new List<EntryModel>();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT dateTime(Time, 'unixepoch'), Note, Filename," +
        " ltrim(strftime('%m', datetime(TIME, 'unixepoch')), 0) AS Month," +
        " ltrim(strftime('%d', datetime(TIME, 'unixepoch')), 0) AS Day" +
        " FROM Joy LEFT JOIN Picture ON Joy.Id == activityid" +
        $" WHERE Month = '{month}' AND Day = '{day}'";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            entries.Add(new Joy
            {
                BabyName = babyName,
                TimeUTC = reader.GetDateTime(0),
                Note = GetString(reader, 1),
                Filename = GetString(reader, 2)
            });
        }

        return entries;
    }

    private static List<EntryModel> GetActivity(long lowerBound, long upperBound,
        string babyName, SqliteConnection connection)
    {
        var entries = new List<EntryModel>();

        var command = connection.CreateCommand();
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
                BabyName = babyName,
                TimeUTC = reader.GetDateTime(0),
                Note = GetString(reader, 1),
                Filename = GetString(reader, 2),
                Duration = reader.GetInt32(3).ToString(),
                OtherActivity = GetString(reader, 4)
            });
        }

        return entries;
    }

    private static List<EntryModel> GetActivity(int day, int month,
        string babyName, SqliteConnection connection)
    {
        var entries = new List<EntryModel>();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT dateTime(Time, 'unixepoch'), Note, Filename, Duration, Name," +
            " ltrim(strftime('%m', datetime(TIME, 'unixepoch')), 0) AS Month," +
            " ltrim(strftime('%d', datetime(TIME, 'unixepoch')), 0) AS Day" +
            " FROM OtherActivity" +
            " LEFT JOIN Picture ON OtherActivity.Id == activityid" +
            " LEFT JOIN OtherActivityDesc ON OtherActivityDesc.Id == DescID" +
            $" WHERE Month = '{month}' AND Day = '{day}'";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            entries.Add(new ActivityModel
            {
                BabyName = babyName,
                TimeUTC = reader.GetDateTime(0),
                Note = GetString(reader, 1),
                Filename = GetString(reader, 2),
                Duration = reader.GetInt32(3).ToString(),
                OtherActivity = GetString(reader, 4)
            });
        }

        return entries;
    }

    private static List<EntryModel> GetMilestone(long lowerBound, long upperBound,
        string babyName, SqliteConnection connection)
    {
        var entries = new List<EntryModel>();

        var command = connection.CreateCommand();
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
                BabyName = babyName,
                TimeUTC = reader.GetDateTime(0),
                Note = GetString(reader, 1),
                Filename = GetString(reader, 2),
                Milestone = GetString(reader, 3)
            });
        }

        return entries;
    }

    private static List<EntryModel> GetMilestone(int day, int month,
        string babyName, SqliteConnection connection)
    {
        var entries = new List<EntryModel>();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT dateTime(Time, 'unixepoch'), Note, Filename, Name," +
            " ltrim(strftime('%m', datetime(TIME, 'unixepoch')), 0) AS Month," +
            " ltrim(strftime('%d', datetime(TIME, 'unixepoch')), 0) AS Day" +
            " FROM Milestone" +
            " LEFT JOIN Picture ON Milestone.Id == activityid" +
            " LEFT JOIN MilestoneSelection ON MilestoneSelection.Id == Milestoneselectionid" +
            $" WHERE Day = '{day}' AND Month = '{month}'";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            entries.Add(new MilestoneModel
            {
                BabyName = babyName,
                TimeUTC = reader.GetDateTime(0),
                Note = GetString(reader, 1),
                Filename = GetString(reader, 2),
                Milestone = GetString(reader, 3)
            });
        }

        return entries;
    }

    private static string GetString(SqliteDataReader reader, int column)
        => reader.IsDBNull(column) ? string.Empty : reader.GetString(column);

    private static long ToUnixTimestamp(DateTime date)
        => new DateTimeOffset(date).ToUnixTimeSeconds();
}
