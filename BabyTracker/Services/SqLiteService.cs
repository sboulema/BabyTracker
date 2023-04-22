using BabyTracker.Extensions;
using BabyTracker.Models.Database;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.SqlQuery;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BabyTracker.Services;

public interface ISqLiteService
{
    DataConnection OpenDataConnection(ClaimsPrincipal user);

    Task<List<IDbEntry>> GetEntriesFromDb(DateOnly date, ClaimsPrincipal user, string babyName);

    Task<List<IDbEntry>> GetMemoriesFromDb(DateTime date, ClaimsPrincipal user, string babyName);

    List<IDbEntry> GetMemoriesFromDb(DateTime date, string userId, string babyName);

    List<Growth> GetGrowth(long lowerBound, long upperBound, string babyName, DataConnection db);

    Task<List<Baby>> GetBabiesFromDb(ClaimsPrincipal user);

    List<Baby> GetBabiesFromDb(string userId);

    Task<DateTime> GetLastEntryDateTime(ClaimsPrincipal user, string babyName);

    Task<List<DateOnly>> GetAllEntryDates(ClaimsPrincipal user, string babyName);

    Task<List<Picture>> GetPictures(ClaimsPrincipal user, string babyName);
}

public class SqLiteService : ISqLiteService
{
    private readonly IWebHostEnvironment _webHostEnvironment;

    public SqLiteService(IWebHostEnvironment webHostEnvironment)
    {
        _webHostEnvironment = webHostEnvironment;
    }

    public DataConnection OpenDataConnection(ClaimsPrincipal user)
    {
        var activeUserId = user.FindFirstValue("activeUserId");

        return OpenDataConnection(activeUserId);
    }

    /// <summary>
    /// Open a connection with the sqlite db file
    /// </summary>
    /// <param name="userId">userId without prefix</param>
    /// <returns></returns>
    public DataConnection OpenDataConnection(string userId)
    {
        var path = $"/data/Data/{userId}/EasyLog.db";

        if (!_webHostEnvironment.IsProduction())
        {
            path = Path.Combine(_webHostEnvironment.ContentRootPath, "Data", userId, "EasyLog.db");
        }

        var db = new DataConnection(
            new DataOptions()
            .UseSQLite($"Data Source={path}"));

        return db;
    }

    public async Task<List<IDbEntry>> GetEntriesFromDb(DateOnly date, ClaimsPrincipal user, string babyName)
    {
        using var dataConnection = OpenDataConnection(user);

        var lowerBound = date.ToDateTime(TimeOnly.MinValue).ToUnixTimestamp();
        var upperBound = date.AddDays(1).ToDateTime(TimeOnly.MinValue).ToUnixTimestamp();

        var entries = new List<IDbEntry>();

        entries.AddRange(GetActivity(lowerBound, upperBound, dataConnection));
        entries.AddRange(GetDiapers(lowerBound, upperBound, dataConnection));
        entries.AddRange(GetFormula(lowerBound, upperBound, dataConnection));
        entries.AddRange(GetGrowth(lowerBound, upperBound, babyName, dataConnection));
        entries.AddRange(GetJoy(lowerBound, upperBound, dataConnection));
        entries.AddRange(GetMedication(lowerBound, upperBound, babyName, dataConnection));
        entries.AddRange(GetMilestone(lowerBound, upperBound, babyName, dataConnection));
        entries.AddRange(GetSleep(lowerBound, upperBound, babyName, dataConnection));
        entries.AddRange(GetSupplement(lowerBound, upperBound, babyName, dataConnection));
        entries.AddRange(GetTemperature(lowerBound, upperBound, babyName, dataConnection));
        entries.AddRange(GetVaccine(lowerBound, upperBound, babyName, dataConnection));

        return entries;
    }

    public async Task<List<IDbEntry>> GetMemoriesFromDb(DateTime date, ClaimsPrincipal user, string babyName)
    {
        using var dataConnection = OpenDataConnection(user);

        var entries = new List<IDbEntry>();

        entries.AddRange(GetActivity(date.Day, date.Month, babyName, dataConnection));
        entries.AddRange(GetJoy(date.Day, date.Month, babyName, dataConnection));
        entries.AddRange(GetMilestone(date.Day, date.Month, babyName, dataConnection));

        return entries;
    }

    public List<IDbEntry> GetMemoriesFromDb(DateTime date, string userId, string babyName)
    {
        using var dataConnection = OpenDataConnection(userId);

        var entries = new List<IDbEntry>();

        entries.AddRange(GetActivity(date.Day, date.Month, babyName, dataConnection));
        entries.AddRange(GetJoy(date.Day, date.Month, babyName, dataConnection));
        entries.AddRange(GetMilestone(date.Day, date.Month, babyName, dataConnection));

        return entries;
    }

    public async Task<List<Baby>> GetBabiesFromDb(ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue("activeUserId");

        if (string.IsNullOrEmpty(userId))
        {
            return new();
        }

        return GetBabiesFromDb(userId);
    }

    public List<Baby> GetBabiesFromDb(string userId)
    {
        using var db = OpenDataConnection(userId);

        var babies = db.GetTable<Baby>().ToList();
        return babies;
    }

    public async Task<DateTime> GetLastEntryDateTime(ClaimsPrincipal user, string babyName)
    {
        using var db = OpenDataConnection(user);

        var timestamp = await db.GetTable<Diaper>().Select(diaper => diaper.Time)
            .Union(db.GetTable<Formula>().Select(formula => formula.Time))
            .Union(db.GetTable<Growth>().Select(growth => growth.Time))
            .Union(db.GetTable<Joy>().Select(joy => joy.Time))
            .Union(db.GetTable<Medication>().Select(medication => medication.Time))
            .Union(db.GetTable<Milestone>().Select(milestone => milestone.Time))
            .Union(db.GetTable<Activity>().Select(activity => activity.Time))
            .Union(db.GetTable<Sleep>().Select(sleep => sleep.Time))
            .Union(db.GetTable<Temperature>().Select(temperature => temperature.Time))
            .Union(db.GetTable<Vaccine>().Select(vaccine => vaccine.Time))
            .OrderByDescending(entry => entry)
            .Take(1)
            .FirstOrDefaultAsync();

        return timestamp.ToDateTimeLocal();
    }

    public async Task<List<DateOnly>> GetAllEntryDates(ClaimsPrincipal user, string babyName)
    {
        using var db = OpenDataConnection(user);

        var timestamps = await db.GetTable<Diaper>().Select(diaper => diaper.Time)
            .Union(db.GetTable<Formula>().Select(formula => formula.Time))
            .Union(db.GetTable<Growth>().Select(growth => growth.Time))
            .Union(db.GetTable<Joy>().Select(joy => joy.Time))
            .Union(db.GetTable<Medication>().Select(medication => medication.Time))
            .Union(db.GetTable<Milestone>().Select(milestone => milestone.Time))
            .Union(db.GetTable<Activity>().Select(activity => activity.Time))
            .Union(db.GetTable<Sleep>().Select(sleep => sleep.Time))
            .Union(db.GetTable<Temperature>().Select(temperature => temperature.Time))
            .Union(db.GetTable<Vaccine>().Select(vaccine => vaccine.Time))
            .ToListAsync();

        return timestamps
            .Select(timestamp => timestamp.ToDateOnly())
            .Distinct()
            .ToList();
    }

    public async Task<List<Picture>> GetPictures(ClaimsPrincipal user, string babyName)
    {
        using var db = OpenDataConnection(user);

        var pictures = db.GetTable<Picture>()
            .OrderByDescending(picture => picture.Timestamp)
            .ToList();

        return pictures;
    }

    private static List<Supplement> GetSupplement(long lowerBound, long upperBound,
        string babyName, DataConnection db)
    {
        var entries = db.GetTable<Supplement>()
            .Where(supplement => supplement.Time >= lowerBound && supplement.Time <= upperBound)
            .LeftJoin(
                db.GetTable<SupplementSelection>(),
                (supplement, supplementSelection) => supplement.TypeId == supplementSelection.Id,
                (supplement, supplementSelection) => new Supplement
                {
                    Time = supplement.Time,
                    Note = supplement.Note,
                    Title = $"{supplement.Amount} {supplement.Unit} {supplementSelection.Name}"
                })
            .ToList();

        return entries;
    }

    private static List<Medication> GetMedication(long lowerBound, long upperBound,
        string babyName, DataConnection db)
    {
        var entries = db.GetTable<Medication>()
            .Where(medication => medication.Time >= lowerBound && medication.Time <= upperBound)
            .LeftJoin(
                db.GetTable<MedicationSelection>(),
                (medication, medicationSelection) => medication.MedicineSelectionId == medicationSelection.Id,
                (medication, medicationSelection) => new Medication
                {
                    Time = medication.Time,
                    Note = medication.Note,
                    MedicationName = medicationSelection.Name,
                    Amount = medication.Amount,
                    AmountPerTime = medicationSelection.AmountPerTime,
                    Unit = medicationSelection.Unit
                })
            .ToList();

        return entries;
    }

    private static List<Vaccine> GetVaccine(long lowerBound, long upperBound,
        string babyName, DataConnection db)
    {
        var entries = db.GetTable<Vaccine>()
            .Where(vaccine => vaccine.Time >= lowerBound && vaccine.Time <= upperBound)
            .LeftJoin(
                db.GetTable<VaccineSelection>(),
                (vaccine, vaccineSelection) => vaccine.VaccineId == vaccineSelection.Id,
                (vaccine, vaccineSelection) => new Vaccine
                {
                    Time = vaccine.Time,
                    BabyName = babyName,
                    Note = vaccine.Note,
                    Title = $"{vaccineSelection.Name} - {vaccineSelection.Description}"
                })
            .ToList();

        return entries;
    }

    private static List<Temperature> GetTemperature(long lowerBound, long upperBound,
        string babyName, DataConnection db)
    {
        var entries = db.GetTable<Temperature>()
            .Where(temperature => temperature.Time >= lowerBound && temperature.Time <= upperBound)
            .ToList();

        return entries;
    }

    private static List<Sleep> GetSleep(long lowerBound, long upperBound,
        string babyName, DataConnection db)
    {
        var entries = db.GetTable<Sleep>()
            .Where(sleep => sleep.Time >= lowerBound && sleep.Time <= upperBound)
            .ToList();

        return entries;
    }

    public List<Growth> GetGrowth(long lowerBound, long upperBound,
        string babyName, DataConnection db)
    {
        var entries = db.GetTable<Growth>()
            .Where(growth => growth.Time >= lowerBound && growth.Time <= upperBound)
            .ToList();

        return entries;
    }

    private static List<Formula> GetFormula(long lowerBound, long upperBound,
        DataConnection db)
    {
        var entries = db.GetTable<Formula>()
            .Where(formula => formula.Time >= lowerBound && formula.Time <= upperBound)
            .ToList();

        return entries;
    }

    private static List<Diaper> GetDiapers(long lowerBound, long upperBound,
        DataConnection db)
    {
        var entries = db.GetTable<Diaper>()
            .Where(diaper => diaper.Time >= lowerBound && diaper.Time <= upperBound)
            .ToList();

        return entries;
    }

    private static List<Joy> GetJoy(long lowerBound, long upperBound,
        DataConnection db)
    {
        var entries = db.GetTable<Joy>()
            .Where(joy => joy.Time >= lowerBound && joy.Time <= upperBound)
            .LeftJoin(
                db.GetTable<Picture>(),
                (joy, picture) => joy.Id == picture.ActivityId,
                (joy, picture) => new Joy
                {
                    Time = joy.Time,
                    Note = joy.Note,
                    FileName = picture.FileName
                })
            .ToList();

        return entries;
    }

    private static List<Joy> GetJoy(int day, int month,
        string babyName, DataConnection db)
    {
        var entries = db.Query<Joy>($"""
            SELECT
                Time,
                Note,
                Filename,
                ltrim(strftime('%m', datetime(TIME, 'unixepoch')), 0) AS Month,
                ltrim(strftime('%d', datetime(TIME, 'unixepoch')), 0) AS Day
            FROM Joy 
            LEFT JOIN Picture ON Joy.Id == activityid 
            WHERE Month = '{month}' and Day = '{day}'
            """)
            .ToList();

        return entries;
    }

    private static List<Activity> GetActivity(long lowerBound, long upperBound,
        DataConnection db)
    {
        var entries = db.GetTable<Activity>()
            .Where(activity => activity.Time >= lowerBound && activity.Time <= upperBound)
            .LeftJoin(
                db.GetTable<ActivityDescription>(),
                (activity, description) => activity.DescriptionId == description.Id,
                (activity, description) => new
                {
                    Activity = activity,
                    Description = description
                })
            .LeftJoin(
                db.GetTable<Picture>(),
                (result, picture) => result.Activity.Id == picture.ActivityId,
                (result, picture) => new Activity
                {
                    Time = result.Activity.Time,
                    Note = result.Activity.Note,
                    FileName = picture.FileName,
                    Name = result.Description.Name
                })
            .ToList();

        return entries;
    }

    private static List<Activity> GetActivity(int day, int month,
        string babyName, DataConnection db)
    {
        var entries = db.Query<Activity>($"""
            SELECT
                Time,
                Note,
                Filename,
                Name,
                ltrim(strftime('%m', datetime(TIME, 'unixepoch')), 0) AS Month,
                ltrim(strftime('%d', datetime(TIME, 'unixepoch')), 0) AS Day
            FROM OtherActivity 
            LEFT JOIN OtherActivityDesc ON OtherActivity.DescID == OtherActivityDesc.ID 
            LEFT JOIN Picture ON OtherActivity.Id == activityid 
            WHERE Month = '{month}' and Day = '{day}'
            """)
            .ToList();

        return entries;
    }

    private static List<Milestone> GetMilestone(long lowerBound, long upperBound,
        string babyName, DataConnection db)
    {
        var entries = db.GetTable<Milestone>()
            .Where(milestone => milestone.Time >= lowerBound && milestone.Time <= upperBound)
            .LeftJoin(
                db.GetTable<MilestoneSelection>(),
                (milestone, milestoneSelection) => milestone.MilestoneSelectionId == milestoneSelection.Id,
                (milestone, milestoneSelection) => new {
                    Milestone = milestone,
                    MilestoneSelection = milestoneSelection
                })
            .LeftJoin(
                db.GetTable<Picture>(),
                (result, picture) => result.Milestone.Id == picture.ActivityId,
                (result, picture) => new Milestone
                {
                    Time = result.Milestone.Time,
                    Note = result.Milestone.Note,
                    Name = result.MilestoneSelection.Name,
                    Filename = picture.FileName
                })
            .ToList();

        return entries;
    }

    private static List<Milestone> GetMilestone(int day, int month,
        string babyName, DataConnection db)
    {
        var entries = db.Query<Milestone>($"""
            SELECT
                Time,
                Note,
                Filename,
                Name,
                ltrim(strftime('%m', datetime(TIME, 'unixepoch')), 0) AS Month,
                ltrim(strftime('%d', datetime(TIME, 'unixepoch')), 0) AS Day
            FROM Milestone 
            LEFT JOIN Picture ON Milestone.Id == activityid 
            LEFT JOIN MilestoneSelection ON MilestoneSelection.Id == Milestoneselectionid 
            WHERE Month = '{month}' and Day = '{day}'
            """)
            .ToList();

        return entries;
    }
}
