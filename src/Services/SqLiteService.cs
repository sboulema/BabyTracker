using BabyTracker.Extensions;
using BabyTracker.Models.Database;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.Mapping;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace BabyTracker.Services;

public interface ISqLiteService
{
    Task OpenDataConnectionAsync(ClaimsPrincipal user);

    Task OpenDataConnectionAsync(string userId);

    Task CloseDataConnection();

    /// <summary>
    /// Decrypts the clone of <paramref name="userId"/>, runs <paramref name="write"/> in a transaction
    /// on a writable in-memory copy, and encrypts the result back over the clone.
    /// The plaintext never touches the disk. Writes to the same clone are serialized.
    /// </summary>
    /// <param name="userId">userId without prefix (the active user id)</param>
    Task AddEntryAsync(string userId, Func<DataConnection, Task> write);

    Task<List<ActivityDescription>> GetActivityDescriptionsFromDb();

    Task<List<MilestoneSelection>> GetMilestoneSelectionsFromDb();

    Task<List<MedicationSelection>> GetMedicationSelectionsFromDb();

    Task<List<VaccineSelection>> GetVaccineSelectionsFromDb();

    Task<List<SupplementSelection>> GetSupplementSelectionsFromDb();

    Task<List<DateOnly>> GetAllEntryDates(string babyName);

    Task<List<Baby>> GetBabiesFromDb();

    Task<List<IDbEntry>> GetEntriesFromDb(DateOnly date, string babyName);

    Task<List<Growth>> GetGrowth(long lowerBound, long upperBound, string babyName);

    Task<DateTime> GetLastEntryDateTime(string babyName);

    Task<List<IMemoryEntry>> GetMemoriesFromDb(DateTime date, string babyName);

    Task<List<Picture>> GetPictures(string babyName);

    Task<List<IDbEntry>> Search(string match, string babyName);
}

public class SqLiteService(IDataClonePathProvider pathProvider,
    IUserDataKeyService userDataKeyService,
    IDataEncryptionService dataEncryptionService) : ISqLiteService, IAsyncDisposable
{
    private const int SqliteHeaderSize = 100;

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> WriteLocks = new();

    /// <summary>
    /// Writes Guids as lowercase text. The app generates its ids with Java's UUID.toString(), which is
    /// lowercase, and it compares ids as text, while Microsoft.Data.Sqlite would write them uppercase.
    /// </summary>
    private static readonly MappingSchema WriteMappingSchema = CreateWriteMappingSchema();

    private DataConnection? _db;

    public async Task OpenDataConnectionAsync(ClaimsPrincipal user)
    {
        var activeUserId = user.FindFirstValue("activeUserId");

        if (string.IsNullOrEmpty(activeUserId))
        {
            return;
        }

        await OpenDataConnectionAsync(activeUserId);
    }

    /// <summary>
    /// Decrypts the clone database and opens it as a read-only, in-memory SQLite database.
    /// The plaintext never touches the disk.
    /// </summary>
    /// <param name="userId">userId without prefix</param>
    public async Task OpenDataConnectionAsync(string userId)
    {
        // Never leak a previously opened in-memory database.
        await CloseDataConnection();

        var path = pathProvider.GetDatabasePath(userId);

        if (!File.Exists(path))
        {
            return;
        }

        byte[]? key = null;
        byte[]? plaintextDatabase = null;

        try
        {
            // Read path: never creates a key.
            key = await userDataKeyService.GetRequiredKeyAsync(userId);

            var encryptedDatabase = await File.ReadAllBytesAsync(path);

            plaintextDatabase = dataEncryptionService.DecryptFile(
                encryptedDatabase,
                key,
                pathProvider.GetAssociatedData(userId, DataClonePathProvider.DatabaseFileName));

            _db = CreateReadOnlyMemoryConnection(plaintextDatabase);
        }
        finally
        {
            if (key is not null)
            {
                CryptographicOperations.ZeroMemory(key);
            }

            if (plaintextDatabase is not null)
            {
                CryptographicOperations.ZeroMemory(plaintextDatabase);
            }
        }
    }

    public async Task CloseDataConnection()
    {
        if (_db == null)
        {
            return;
        }

        await _db.CloseAsync();
        await _db.DisposeAsync();

        _db = null;
    }

    public async ValueTask DisposeAsync()
        => await CloseDataConnection();

    public async Task AddEntryAsync(string userId, Func<DataConnection, Task> write)
    {
        var writeLock = WriteLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));

        await writeLock.WaitAsync();

        byte[]? key = null;
        byte[]? plaintextDatabase = null;
        byte[]? updatedDatabase = null;

        try
        {
            var path = pathProvider.GetDatabasePath(userId);

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("The data clone database does not exist.", path);
            }

            var associatedData = pathProvider.GetAssociatedData(userId, DataClonePathProvider.DatabaseFileName);

            key = await userDataKeyService.GetRequiredKeyAsync(userId);

            plaintextDatabase = dataEncryptionService.DecryptFile(
                await File.ReadAllBytesAsync(path),
                key,
                associatedData);

            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            DeserializeDatabase(connection, plaintextDatabase, readOnly: false);

            await using (var db = new DataConnection(
                new DataOptions()
                    .UseConnection(
                        SQLiteTools.GetDataProvider(SQLiteProvider.Microsoft),
                        connection,
                        disposeConnection: false)
                    .UseMappingSchema(WriteMappingSchema)))
            {
                // Disposing the transaction without committing rolls the changes back
                using var transaction = await db.BeginTransactionAsync();

                await write(db);

                await transaction.CommitAsync();
            }

            updatedDatabase = SerializeDatabase(connection);

            using var updatedStream = new MemoryStream(updatedDatabase);

            // Encrypts and replaces the clone atomically
            dataEncryptionService.EncryptStream(updatedStream, path, key, associatedData);
        }
        finally
        {
            if (key is not null)
            {
                CryptographicOperations.ZeroMemory(key);
            }

            if (plaintextDatabase is not null)
            {
                CryptographicOperations.ZeroMemory(plaintextDatabase);
            }

            if (updatedDatabase is not null)
            {
                CryptographicOperations.ZeroMemory(updatedDatabase);
            }

            writeLock.Release();
        }
    }

    private static MappingSchema CreateWriteMappingSchema()
    {
        var schema = new MappingSchema();

        schema.SetConverter<Guid, DataParameter>(guid => new DataParameter
        {
            DataType = DataType.NVarChar,
            Value = guid.ToString("D")
        });

        return schema;
    }

    private static DataConnection CreateReadOnlyMemoryConnection(byte[] plaintextDatabase)
    {
        var connection = new SqliteConnection("Data Source=:memory:");

        try
        {
            connection.Open();
            DeserializeDatabase(connection, plaintextDatabase);

            return new DataConnection(
                new DataOptions()
                    .UseConnection(
                        SQLiteTools.GetDataProvider(SQLiteProvider.Microsoft),
                        connection,
                        disposeConnection: true));
        }
        catch
        {
            connection.Dispose();

            throw;
        }
    }

    /// <summary>
    /// Hands the database image to SQLite as the contents of the in-memory database.
    /// SQLite takes ownership of the buffer (SQLITE_DESERIALIZE_FREEONCLOSE) and frees it when
    /// the connection closes, also when deserialization fails.
    /// </summary>
    private static void DeserializeDatabase(SqliteConnection connection, byte[] plaintextDatabase, bool readOnly = true)
    {
        if (plaintextDatabase.Length < SqliteHeaderSize)
        {
            throw new InvalidDataException("The data clone database is not a valid SQLite database.");
        }

        // A database image in WAL mode cannot be opened from memory ("unable to open database
        // file"). Bytes 18 and 19 of the header are the read and write format versions
        // (1 = rollback journal, 2 = WAL); marking them 1 makes SQLite read the image as-is.
        // The image holds a checkpointed database, as the previous backup-based approach assumed.
        if (plaintextDatabase[18] == 2)
        {
            plaintextDatabase[18] = 1;
        }

        if (plaintextDatabase[19] == 2)
        {
            plaintextDatabase[19] = 1;
        }

        var size = plaintextDatabase.Length;
        var buffer = SQLitePCL.raw.sqlite3_malloc64(size);

        if (buffer == IntPtr.Zero)
        {
            throw new OutOfMemoryException("Could not allocate memory for the data clone database.");
        }

        Marshal.Copy(plaintextDatabase, 0, buffer, size);

        var result = SQLitePCL.raw.sqlite3_deserialize(
            connection.Handle!,
            "main",
            buffer,
            size,
            size,
            SQLitePCL.raw.SQLITE_DESERIALIZE_FREEONCLOSE |
            (readOnly
                ? SQLitePCL.raw.SQLITE_DESERIALIZE_READONLY
                : SQLitePCL.raw.SQLITE_DESERIALIZE_RESIZEABLE));

        if (result != SQLitePCL.raw.SQLITE_OK)
        {
            throw new InvalidOperationException($"SQLite could not load the data clone database (result code {result}).");
        }
    }

    /// <summary>Copies the in-memory database image out of SQLite.</summary>
    private static byte[] SerializeDatabase(SqliteConnection connection)
    {
        var buffer = SQLitePCL.raw.sqlite3_serialize(connection.Handle!, "main", out var size, 0);

        if (buffer == IntPtr.Zero)
        {
            throw new InvalidOperationException("SQLite could not serialize the data clone database.");
        }

        try
        {
            var bytes = new byte[size];
            Marshal.Copy(buffer, bytes, 0, (int)size);

            return bytes;
        }
        finally
        {
            SQLitePCL.raw.sqlite3_free(buffer);
        }
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

    public async Task<List<ActivityDescription>> GetActivityDescriptionsFromDb()
    {
        if (_db == null)
        {
            return [];
        }

        return await _db.GetTable<ActivityDescription>()
            .OrderBy(description => description.Name)
            .ToListAsync();
    }

    public async Task<List<MilestoneSelection>> GetMilestoneSelectionsFromDb()
    {
        if (_db == null)
        {
            return [];
        }

        return await _db.GetTable<MilestoneSelection>()
            .OrderBy(selection => selection.Name)
            .ToListAsync();
    }

    public async Task<List<MedicationSelection>> GetMedicationSelectionsFromDb()
    {
        if (_db == null)
        {
            return [];
        }

        return await _db.GetTable<MedicationSelection>()
            .OrderBy(selection => selection.Name)
            .ToListAsync();
    }

    public async Task<List<VaccineSelection>> GetVaccineSelectionsFromDb()
    {
        if (_db == null)
        {
            return [];
        }

        return await _db.GetTable<VaccineSelection>()
            .OrderBy(selection => selection.Name)
            .ToListAsync();
    }

    public async Task<List<SupplementSelection>> GetSupplementSelectionsFromDb()
    {
        if (_db == null)
        {
            return [];
        }

        return await _db.GetTable<SupplementSelection>()
            .OrderBy(selection => selection.Name)
            .ToListAsync();
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
            .Select(picture => new Picture
            {
                Timestamp = picture.Timestamp,
                ActivityId = picture.ActivityId,
                FileName = picture.FileName
            })
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
