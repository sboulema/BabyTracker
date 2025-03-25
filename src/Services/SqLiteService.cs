using BabyTracker.Extensions;
using BabyTracker.Models.Database;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SQLite;
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
    void OpenDataConnection(ClaimsPrincipal user);

    void OpenDataConnection(string userId);

    Task CloseDataConnection();

    Task<List<DateOnly>> GetAllEntryDates(string babyName);

    Task<List<Baby>> GetBabiesFromDb();

    Task<List<IDbEntry>> GetEntriesFromDb(DateOnly date, string babyName);

    Task<List<Growth>> GetGrowth(long lowerBound, long upperBound, string babyName);

    Task<DateTime> GetLastEntryDateTime(string babyName);

    Task<List<IMemoryEntry>> GetMemoriesFromDb(DateTime date, string babyName);

    Task<List<Picture>> GetPictures(string babyName);

    Task<List<IDbEntry>> Search(string match, string babyName);
}

public class SqLiteService(IWebHostEnvironment webHostEnvironment) : ISqLiteService
{
    private DataConnection? _db;

    public void OpenDataConnection(ClaimsPrincipal user)
    {
        var activeUserId = user.FindFirstValue("activeUserId");

        if (string.IsNullOrEmpty(activeUserId))
        {
            return;
        }

        OpenDataConnection(activeUserId);
    }

    /// <summary>
    /// Open a connection with the SQLite db file
    /// </summary>
    /// <param name="userId">userId without prefix</param>
    /// <returns></returns>
    public void OpenDataConnection(string userId)
    {
        var path = $"/data/Data/{userId}/EasyLog.db";

        if (!webHostEnvironment.IsProduction())
        {
            path = Path.Combine(webHostEnvironment.ContentRootPath, "Data", userId, "EasyLog.db");
        }

        if (!Path.Exists(path))
        {
            return;
        }

        _db = new DataConnection(
            new DataOptions()
            .UseSQLite($"Data Source={path}"));
    }

    public async Task CloseDataConnection()
    {
        if (_db == null)
        {
            return;
        }

        await _db.CloseAsync();
    }

    public async Task<List<IDbEntry>> GetEntriesFromDb(DateOnly date, string babyName)
    {
        var lowerBound = date.ToDateTime(TimeOnly.MinValue).ToUnixTimestamp();
        var upperBound = date.AddDays(1).ToDateTime(TimeOnly.MinValue).ToUnixTimestamp();

        var entries = new List<IDbEntry>();

        entries.AddRange(await GetActivity(lowerBound, upperBound));
        entries.AddRange(await GetDiapers(lowerBound, upperBound));
        entries.AddRange(await GetFormula(lowerBound, upperBound));
        entries.AddRange(await GetGrowth(lowerBound, upperBound, babyName));
        entries.AddRange(await GetJoy(lowerBound, upperBound));
        entries.AddRange(await GetMedication(lowerBound, upperBound, babyName));
        entries.AddRange(await GetMilestone(lowerBound, upperBound, babyName));
        entries.AddRange(await GetSleep(lowerBound, upperBound, babyName));
        entries.AddRange(await GetSupplement(lowerBound, upperBound, babyName));
        entries.AddRange(await GetTemperature(lowerBound, upperBound, babyName));
        entries.AddRange(await GetVaccine(lowerBound, upperBound, babyName));

        return entries;
    }

    public async Task<List<IMemoryEntry>> GetMemoriesFromDb(DateTime date, string babyName)
    {
        var entries = new List<IMemoryEntry>();

        entries.AddRange(await GetActivity(date.Day, date.Month, babyName));
        entries.AddRange(await GetJoy(date.Day, date.Month, babyName));
        entries.AddRange(await GetMilestone(date.Day, date.Month, babyName));

        entries.RemoveAll(entry => string.IsNullOrEmpty(entry.Note) &&
                                   string.IsNullOrEmpty(entry.FileName));

        return entries;
    }

    public async Task<List<Baby>> GetBabiesFromDb()
    {
        if (_db == null)
        {
            return [];
        }

        var babies = await _db.GetTable<Baby>()
            .ToListAsync();

        return babies;
    }

    public async Task<DateTime> GetLastEntryDateTime(string babyName)
    {
        var timestamp = await _db.GetTable<Diaper>().Select(diaper => diaper.Time)
            .Union(_db.GetTable<Formula>().Select(formula => formula.Time))
            .Union(_db.GetTable<Growth>().Select(growth => growth.Time))
            .Union(_db.GetTable<Joy>().Select(joy => joy.Time))
            .Union(_db.GetTable<Medication>().Select(medication => medication.Time))
            .Union(_db.GetTable<Milestone>().Select(milestone => milestone.Time))
            .Union(_db.GetTable<Activity>().Select(activity => activity.Time))
            .Union(_db.GetTable<Sleep>().Select(sleep => sleep.Time))
            .Union(_db.GetTable<Temperature>().Select(temperature => temperature.Time))
            .Union(_db.GetTable<Vaccine>().Select(vaccine => vaccine.Time))
            .OrderByDescending(entry => entry)
            .Take(1)
            .FirstOrDefaultAsync();

        return timestamp.ToDateTimeLocal();
    }

    public async Task<List<DateOnly>> GetAllEntryDates(string babyName)
    {
        var timestamps = await _db.GetTable<Diaper>().Select(diaper => diaper.Time)
            .Union(_db.GetTable<Formula>().Select(formula => formula.Time))
            .Union(_db.GetTable<Growth>().Select(growth => growth.Time))
            .Union(_db.GetTable<Joy>().Select(joy => joy.Time))
            .Union(_db.GetTable<Medication>().Select(medication => medication.Time))
            .Union(_db.GetTable<Milestone>().Select(milestone => milestone.Time))
            .Union(_db.GetTable<Activity>().Select(activity => activity.Time))
            .Union(_db.GetTable<Sleep>().Select(sleep => sleep.Time))
            .Union(_db.GetTable<Temperature>().Select(temperature => temperature.Time))
            .Union(_db.GetTable<Vaccine>().Select(vaccine => vaccine.Time))
            .ToListAsync();

        return timestamps
            .Select(timestamp => timestamp.ToDateOnly())
            .Distinct()
            .ToList();
    }

    public async Task<List<Picture>> GetPictures(string babyName)
    {
        var pictures = await _db.GetTable<Picture>()
            .OrderByDescending(picture => picture.Timestamp)
            .ToListAsync();

        return pictures;
    }

    public async Task<List<IDbEntry>> Search(string match, string babyName)
    {
        var entries = new List<IDbEntry>();

        entries.AddRange(await SearchJoy(match));
        entries.AddRange(await SearchMilestone(match, babyName));
        entries.AddRange(await SearchActivity(match));

        return entries;
    }

    private async Task<List<Supplement>> GetSupplement(long lowerBound, long upperBound,
        string babyName)
    {
        var entries = await _db.GetTable<Supplement>()
            .Where(supplement => supplement.Time >= lowerBound && supplement.Time <= upperBound)
            .LeftJoin(
                _db.GetTable<SupplementSelection>(),
                (supplement, supplementSelection) => supplement.TypeId == supplementSelection.Id,
                (supplement, supplementSelection) => new Supplement
                {
                    Time = supplement.Time,
                    Note = supplement.Note,
                    Title = $"{supplementSelection.Name} {supplement.Amount} {supplement.Unit}"
                })
            .ToListAsync();

        return entries;
    }

    private async Task<List<Medication>> GetMedication(long lowerBound, long upperBound,
        string babyName)
    {
        var entries = await _db.GetTable<Medication>()
            .Where(medication => medication.Time >= lowerBound && medication.Time <= upperBound)
            .LeftJoin(
                _db.GetTable<MedicationSelection>(),
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

    private async Task<List<Vaccine>> GetVaccine(long lowerBound, long upperBound,
        string babyName)
    {
        var entries = await _db.GetTable<Vaccine>()
            .Where(vaccine => vaccine.Time >= lowerBound && vaccine.Time <= upperBound)
            .LeftJoin(
                _db.GetTable<VaccineSelection>(),
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

    private async Task<List<Temperature>> GetTemperature(long lowerBound, long upperBound,
        string babyName)
    {
        var entries = await _db.GetTable<Temperature>()
            .Where(temperature => temperature.Time >= lowerBound && temperature.Time <= upperBound)
            .ToListAsync();

        return entries;
    }

    private async Task<List<Sleep>> GetSleep(long lowerBound, long upperBound,
        string babyName)
    {
        var entries = await _db.GetTable<Sleep>()
            .Where(sleep => sleep.Time >= lowerBound && sleep.Time <= upperBound)
            .ToListAsync();

        return entries;
    }

    public async Task<List<Growth>> GetGrowth(long lowerBound, long upperBound,
        string babyName)
    {
        var entries = await _db.GetTable<Growth>()
            .Where(growth => growth.Time >= lowerBound && growth.Time <= upperBound)
            .ToListAsync();

        return entries;
    }

    private async Task<List<Formula>> GetFormula(long lowerBound, long upperBound)
    {
        var entries = await _db.GetTable<Formula>()
            .Where(formula => formula.Time >= lowerBound && formula.Time <= upperBound)
            .ToListAsync();

        return entries;
    }

    private async Task<List<Diaper>> GetDiapers(long lowerBound, long upperBound)
    {
        var entries = await _db.GetTable<Diaper>()
            .Where(diaper => diaper.Time >= lowerBound && diaper.Time <= upperBound)
            .ToListAsync();

        return entries;
    }

    private async Task<List<Joy>> GetJoy(long lowerBound, long upperBound)
    {
        var entries = await _db.GetTable<Joy>()
            .Where(joy => joy.Time >= lowerBound && joy.Time <= upperBound)
            .LeftJoin(
                _db.GetTable<Picture>(),
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

    private async Task<List<Joy>> SearchJoy(string match)
    {
        var entries = await _db.GetTable<Joy>()
            .Where(joy => joy.Note.Contains(match))
            .LeftJoin(
                _db.GetTable<Picture>(),
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

    private async Task<List<Joy>> GetJoy(int day, int month,
        string babyName)
    {
        var entries = await _db.QueryToListAsync<Joy>($"""
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

    private async Task<List<Activity>> GetActivity(long lowerBound, long upperBound)
    {
        var entries = await _db.GetTable<Activity>()
            .Where(activity => activity.Time >= lowerBound && activity.Time <= upperBound)
            .LeftJoin(
                _db.GetTable<ActivityDescription>(),
                (activity, description) => activity.DescriptionId == description.Id,
                (activity, description) => new
                {
                    Activity = activity,
                    Description = description
                })
            .LeftJoin(
                _db.GetTable<Picture>(),
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

    private async Task<List<Activity>> SearchActivity(string match)
    {
        var entries = await _db.GetTable<Activity>()
            .Where(activity => activity.Note.Contains(match))
            .LeftJoin(
                _db.GetTable<ActivityDescription>(),
                (activity, description) => activity.DescriptionId == description.Id,
                (activity, description) => new {
                    Activity = activity,
                    Description = description
                })
            .LeftJoin(
                _db.GetTable<Picture>(),
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

    private async Task<List<Activity>> GetActivity(int day, int month,
        string babyName)
    {
        var entries = await _db.QueryToListAsync<Activity>($"""
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

    private async Task<List<Milestone>> GetMilestone(long lowerBound, long upperBound,
        string babyName)
    {
        var entries = await _db.GetTable<Milestone>()
            .Where(milestone => milestone.Time >= lowerBound && milestone.Time <= upperBound)
            .LeftJoin(
                _db.GetTable<MilestoneSelection>(),
                (milestone, milestoneSelection) => milestone.MilestoneSelectionId == milestoneSelection.Id,
                (milestone, milestoneSelection) => new {
                    Milestone = milestone,
                    MilestoneSelection = milestoneSelection
                })
            .LeftJoin(
                _db.GetTable<Picture>(),
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

    private async Task<List<Milestone>> SearchMilestone(string match,
        string babyName)
    {
        var entries = await _db.GetTable<Milestone>()
            .Where(milestone => milestone.Note.Contains(match))
            .LeftJoin(
                _db.GetTable<MilestoneSelection>(),
                (milestone, milestoneSelection) => milestone.MilestoneSelectionId == milestoneSelection.Id,
                (milestone, milestoneSelection) => new {
                    Milestone = milestone,
                    MilestoneSelection = milestoneSelection
                })
            .LeftJoin(
                _db.GetTable<Picture>(),
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

    private async Task<List<Milestone>> GetMilestone(int day, int month,
        string babyName)
    {
        var entries = await _db.QueryToListAsync<Milestone>($"""
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
