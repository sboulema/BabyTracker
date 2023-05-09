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
    DataConnection? OpenDataConnection(ClaimsPrincipal user);

    DataConnection OpenDataConnection(string userId);
}

public class SqLiteService : ISqLiteService
{
    private readonly IWebHostEnvironment _webHostEnvironment;

    public SqLiteService(IWebHostEnvironment webHostEnvironment)
    {
        _webHostEnvironment = webHostEnvironment;
    }

    public DataConnection? OpenDataConnection(ClaimsPrincipal user)
    {
        var activeUserId = user.FindFirstValue("activeUserId");

        if (string.IsNullOrEmpty(activeUserId))
        {
            return null;
        }

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

    public static async Task<List<IDbEntry>> GetEntriesFromDb(DateOnly date, string babyName, DataConnection db)
    {
        var lowerBound = date.ToDateTime(TimeOnly.MinValue).ToUnixTimestamp();
        var upperBound = date.AddDays(1).ToDateTime(TimeOnly.MinValue).ToUnixTimestamp();

        var entries = new List<IDbEntry>();

        entries.AddRange(await GetActivity(lowerBound, upperBound, db));
        entries.AddRange(await GetDiapers(lowerBound, upperBound, db));
        entries.AddRange(await GetFormula(lowerBound, upperBound, db));
        entries.AddRange(await GetGrowth(lowerBound, upperBound, babyName, db));
        entries.AddRange(await GetJoy(lowerBound, upperBound, db));
        entries.AddRange(await GetMedication(lowerBound, upperBound, babyName, db));
        entries.AddRange(await GetMilestone(lowerBound, upperBound, babyName, db));
        entries.AddRange(await GetSleep(lowerBound, upperBound, babyName, db));
        entries.AddRange(await GetSupplement(lowerBound, upperBound, babyName, db));
        entries.AddRange(await GetTemperature(lowerBound, upperBound, babyName, db));
        entries.AddRange(await GetVaccine(lowerBound, upperBound, babyName, db));

        return entries;
    }

    public static async Task<List<IMemoryEntry>> GetMemoriesFromDb(DateTime date, string babyName, DataConnection db)
    {
        var entries = new List<IMemoryEntry>();

        entries.AddRange(await GetActivity(date.Day, date.Month, babyName, db));
        entries.AddRange(await GetJoy(date.Day, date.Month, babyName, db));
        entries.AddRange(await GetMilestone(date.Day, date.Month, babyName, db));

        entries.RemoveAll(entry => string.IsNullOrEmpty(entry.Note) &&
                                   string.IsNullOrEmpty(entry.FileName));

        return entries;
    }

    public static async Task<List<Baby>> GetBabiesFromDb(DataConnection db)
    {
        var babies = await db.GetTable<Baby>()
            .ToListAsync();

        return babies;
    }

    public static async Task<DateTime> GetLastEntryDateTime(string babyName, DataConnection db)
    {
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

    public static async Task<List<DateOnly>> GetAllEntryDates(string babyName, DataConnection db)
    {
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

    public static async Task<List<Picture>> GetPictures(string babyName, DataConnection db)
    {
        var pictures = await db.GetTable<Picture>()
            .OrderByDescending(picture => picture.Timestamp)
            .ToListAsync();

        return pictures;
    }

    private static async Task<List<Supplement>> GetSupplement(long lowerBound, long upperBound,
        string babyName, DataConnection db)
    {
        var entries = await db.GetTable<Supplement>()
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
            .ToListAsync();

        return entries;
    }

    private static async Task<List<Medication>> GetMedication(long lowerBound, long upperBound,
        string babyName, DataConnection db)
    {
        var entries = await db.GetTable<Medication>()
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
            .ToListAsync();

        return entries;
    }

    private static async Task<List<Vaccine>> GetVaccine(long lowerBound, long upperBound,
        string babyName, DataConnection db)
    {
        var entries = await db.GetTable<Vaccine>()
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
            .ToListAsync();

        return entries;
    }

    private static async Task<List<Temperature>> GetTemperature(long lowerBound, long upperBound,
        string babyName, DataConnection db)
    {
        var entries = await db.GetTable<Temperature>()
            .Where(temperature => temperature.Time >= lowerBound && temperature.Time <= upperBound)
            .ToListAsync();

        return entries;
    }

    private static async Task<List<Sleep>> GetSleep(long lowerBound, long upperBound,
        string babyName, DataConnection db)
    {
        var entries = await db.GetTable<Sleep>()
            .Where(sleep => sleep.Time >= lowerBound && sleep.Time <= upperBound)
            .ToListAsync();

        return entries;
    }

    public static async Task<List<Growth>> GetGrowth(long lowerBound, long upperBound,
        string babyName, DataConnection db)
    {
        var entries = await db.GetTable<Growth>()
            .Where(growth => growth.Time >= lowerBound && growth.Time <= upperBound)
            .ToListAsync();

        return entries;
    }

    private static async Task<List<Formula>> GetFormula(long lowerBound, long upperBound,
        DataConnection db)
    {
        var entries = await db.GetTable<Formula>()
            .Where(formula => formula.Time >= lowerBound && formula.Time <= upperBound)
            .ToListAsync();

        return entries;
    }

    private static async Task<List<Diaper>> GetDiapers(long lowerBound, long upperBound,
        DataConnection db)
    {
        var entries = await db.GetTable<Diaper>()
            .Where(diaper => diaper.Time >= lowerBound && diaper.Time <= upperBound)
            .ToListAsync();

        return entries;
    }

    private static async Task<List<Joy>> GetJoy(long lowerBound, long upperBound,
        DataConnection db)
    {
        var entries = await db.GetTable<Joy>()
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
            .ToListAsync();

        return entries;
    }

    private static async Task<List<Joy>> GetJoy(int day, int month,
        string babyName, DataConnection db)
    {
        var entries = await db.QueryToListAsync<Joy>($"""
            SELECT
                Time,
                Note,
                Filename,
                ltrim(strftime('%m', datetime(TIME, 'unixepoch')), 0) AS Month,
                ltrim(strftime('%d', datetime(TIME, 'unixepoch')), 0) AS Day
            FROM Joy 
            LEFT JOIN Picture ON Joy.Id == activityid 
            WHERE Month = '{month}' and Day = '{day}'
            """);

        return entries;
    }

    private static async Task<List<Activity>> GetActivity(long lowerBound, long upperBound,
        DataConnection db)
    {
        var entries = await db.GetTable<Activity>()
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
            .ToListAsync();

        return entries;
    }

    private static async Task<List<Activity>> GetActivity(int day, int month,
        string babyName, DataConnection db)
    {
        var entries = await db.QueryToListAsync<Activity>($"""
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
            """);

        return entries;
    }

    private static async Task<List<Milestone>> GetMilestone(long lowerBound, long upperBound,
        string babyName, DataConnection db)
    {
        var entries = await db.GetTable<Milestone>()
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
                    FileName = picture.FileName
                })
            .ToListAsync();

        return entries;
    }

    private static async Task<List<Milestone>> GetMilestone(int day, int month,
        string babyName, DataConnection db)
    {
        var entries = await db.QueryToListAsync<Milestone>($"""
            SELECT
                Time,
                Note,
                Filename,
                Name,
                ltrim(strftime('%m', datetime(TIME, 'unixepoch')), 0) AS Month,
                ltrim(strftime('%d', datetime(TIME, 'unixepoch')), 0) AS Day
            FROM Milestone 
            LEFT JOIN Picture ON Milestone.Id == activityid 
            LEFT JOIN MilestoneSelection ON Milestone.MilestoneSelectionID == MilestoneSelection.ID 
            WHERE Month = '{month}' and Day = '{day}'
            """);

        return entries;
    }
}
