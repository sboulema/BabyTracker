using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using BabyTracker.Constants;
using BabyTracker.Extensions;
using BabyTracker.Models.Database;
using BabyTracker.Models.ViewModels;
using BabyTracker.Sync;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Office2010.Excel;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Metrics;

namespace BabyTracker.Services;

/// <param name="Sync">What happened with sending the entry to the Baby Tracker server.</param>
public sealed record AddEntryResult(SyncResult Sync);

public interface IAddEntryService
{
    /// <param name="userId">The active user id: the owner of the data clone to write to.</param>
    /// <param name="picture">The processed photo of the entry. Only used by the entry types that can have a photo.</param>
    /// <exception cref="KeyNotFoundException">There is no baby with this name in the data clone.</exception>
    Task<AddEntryResult> AddEntryAsync(string userId, string babyName, AddEntryViewModel model, ProcessedPicture? picture = null);

    /// <summary>Reads an entry of the baby into the form model. The data connection of <see cref="ISqLiteService"/> must be open.</summary>
    /// <returns>The model with the values of the entry, or null when the baby or the entry does not exist.</returns>
    Task<AddEntryViewModel?> GetEntryAsync(string babyName, EntryTypeEnum type, Guid id);

    /// <summary>
    /// Saves the values of <paramref name="model"/> over the entry <paramref name="id"/> of the type <c>model.Type</c>.
    /// The type and the baby of an entry do not change.
    /// </summary>
    /// <param name="picture">A new photo that replaces the current one. Only used by the entry types that can have a photo, null keeps the current photo.</param>
    /// <param name="userId">The active user id: the owner of the data clone to write to.</param>
    /// <exception cref="KeyNotFoundException">There is no baby with this name, or the baby has no such entry.</exception>
    Task<AddEntryResult> UpdateEntryAsync(string userId, string babyName, Guid id, AddEntryViewModel model, ProcessedPicture? picture = null);
}

public class AddEntryService(
    ISqLiteService sqLiteService,
    IPictureService pictureService,
    ISyncService syncService) : IAddEntryService
{
    private const string ThumbnailSuffix = "__thumbnail";

    public async Task<AddEntryResult> AddEntryAsync(string userId, string babyName, AddEntryViewModel model,
        ProcessedPicture? picture = null)
    {
        var syncEnabled = await syncService.IsEnabledAsync(userId);

        Guid? pictureId = null;
        string? pictureFileName = null;
        SyncEntry? syncEntry = null;

        try
        {
            if (picture is not null && model.Type.SupportsPicture())
            {
                // A new picture of the app is named after its own id
                pictureId = Guid.NewGuid();
                pictureFileName = pictureId.Value.ToString("D");

                // The files go first: a failure afterwards only leaves files behind, which are removed below,
                // instead of a Picture row without a file.
                await pictureService.SavePictureAsync(userId, pictureFileName, picture.Jpeg);
                await pictureService.SavePictureAsync(userId, $"{pictureFileName}{ThumbnailSuffix}", picture.Thumbnail);
            }

            await sqLiteService.AddEntryAsync(userId, async db =>
            {
                syncEntry = await InsertEntryAsync(db, babyName, model, pictureId, pictureFileName, picture, syncEnabled);
            });
        }
        catch
        {
            if (pictureFileName is not null)
            {
                pictureService.DeletePicture(userId, pictureFileName);
                pictureService.DeletePicture(userId, $"{pictureFileName}{ThumbnailSuffix}");
            }

            throw;
        }

        if (syncEntry is null)
        {
            return new AddEntryResult(new SyncResult(SyncStatus.Skipped));
        }

        // The entry is in the clone now, so a problem with the Baby Tracker server is only reported
        return new AddEntryResult(await syncService.PublishAsync(userId, syncEntry));
    }

    /// <returns>The entry as a transaction for the Baby Tracker server, or null when <paramref name="buildSyncEntry"/> is false.</returns>
    private static async Task<SyncEntry?> InsertEntryAsync(LinqToDB.Data.DataConnection db, string babyName, AddEntryViewModel model,
        Guid? pictureId, string? pictureFileName, ProcessedPicture? picture, bool buildSyncEntry)
    {
        var baby = await GetBabyAsync(db, babyName);

        var babyId = baby.Id;
        var id = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var time = model.Time.ToUnixTimestampFromLocal();
        var note = model.Note ?? string.Empty;

        var common = new SyncCommon(
            id,
            timestamp,
            time,
            note,
            TimeExtensions.GetCloneTimeZoneRawOffsetSeconds(),
            ToSyncBaby(baby));

        // A photo belongs to the entry: a Picture row that points at it, and HasPicture on the entry
        var hasPicture = picture is not null && pictureId is not null && pictureFileName is not null;
        
        if (hasPicture)
        {
            common = common with { Picture = new SyncPictureSnapshot(pictureId!.Value, pictureFileName!) };
        }
        
        var hasPictureFlag = hasPicture ? 1 : 0;

        async Task<SyncSelectionSnapshot?> ReadSelection(string table, string? selectionId)
            => buildSyncEntry
                ? await SyncSnapshotReader.ReadSelectionAsync(db, table, selectionId ?? string.Empty)
                : null;

        JsonObject? transaction = null;

        switch (model.Type)
        {
            case EntryTypeEnum.Diaper:
                await db.InsertAsync(new Diaper
                {
                    Id = id, Timestamp = timestamp, HasPicture = 0, BabyId = babyId,
                    Time = time, Note = note, Status = model.DiaperStatus
                });

                transaction = SyncTransactionBuilder.Diaper(common, ToSyncDiaperStatus(model.DiaperStatus));
                break;

            case EntryTypeEnum.Formula:
                await db.InsertAsync(new Formula
                {
                    Id = id, Timestamp = timestamp, HasPicture = 0, BabyId = babyId,
                    Time = time, Note = note,
                    Amount = model.FormulaAmount ?? 0,
                    IsEnglishScale = 0
                });

                transaction = SyncTransactionBuilder.Formula(common, model.FormulaAmount ?? 0);
                break;

            case EntryTypeEnum.Sleep:
                await db.InsertAsync(new Sleep
                {
                    Id = id, Timestamp = timestamp, HasPicture = 0, BabyId = babyId,
                    Time = time, Note = note,
                    Duration = model.SleepMinutes ?? 0
                });

                transaction = SyncTransactionBuilder.Sleep(common, model.SleepMinutes ?? 0);
                break;

            case EntryTypeEnum.Joy:
                await db.InsertAsync(new Joy
                {
                    Id = id,
                    Timestamp = timestamp,
                    HasPicture = hasPictureFlag,
                    BabyId = babyId,
                    Time = time, Note = note
                });

                transaction = SyncTransactionBuilder.Joy(common);
                break;

            case EntryTypeEnum.Activity:
            {
                var description = await ReadSelection(SyncSnapshotReader.ActivityDescriptionTable, model.ActivityDescriptionId);

                await db.InsertAsync(new Activity
                {
                    Id = id,
                    Timestamp = timestamp,
                    HasPicture = hasPictureFlag,
                    BabyId = babyId,
                    Time = time, Note = note,
                    DescriptionId = model.ActivityDescriptionId ?? string.Empty,
                    Duration = model.ActivityMinutes ?? 0
                });

                transaction = description is null
                    ? null
                    : SyncTransactionBuilder.OtherActivity(common, description, model.ActivityMinutes ?? 0);
                break;
            }

            case EntryTypeEnum.Growth:
                await db.InsertAsync(new Growth
                {
                    Id = id, Timestamp = timestamp, HasPicture = 0, BabyId = babyId,
                    Time = time, Note = note,
                    Weight = model.GrowthWeight ?? 0,
                    Length = model.GrowthLength ?? 0,
                    HeadSize = model.GrowthHeadSize ?? 0,
                    IsEnglishLengthScale = 0,
                    IsEnglishWeightScale = 0
                });

                transaction = SyncTransactionBuilder.Growth(
                    common, model.GrowthWeight ?? 0, model.GrowthLength ?? 0, model.GrowthHeadSize ?? 0);
                break;

            case EntryTypeEnum.Temperature:
                await db.InsertAsync(new Temperature
                {
                    Id = id, Timestamp = timestamp, HasPicture = 0, BabyId = babyId,
                    Time = time, Note = note,
                    Temp = model.TemperatureCelsius ?? 0,
                    IsEnglishScale = 0
                });

                transaction = SyncTransactionBuilder.Temperature(common, model.TemperatureCelsius ?? 0);
                break;

            case EntryTypeEnum.Milestone:
            {
                var selection = await ReadSelection(SyncSnapshotReader.MilestoneSelectionTable, model.MilestoneSelectionId);

                await db.InsertAsync(new Milestone
                {
                    Id = id,
                    Timestamp = timestamp,
                    HasPicture = hasPictureFlag,
                    BabyId = babyId,
                    Time = time, Note = note,
                    MilestoneSelectionId = model.MilestoneSelectionId,
                });

                transaction = selection is null ? null : SyncTransactionBuilder.Milestone(common, selection);
                break;
            }

            case EntryTypeEnum.Medication:
            {
                var selectionId = model.MedicationSelectionId ?? Guid.Empty;

                // Default to the dose that is set up for the medication
                var amount = model.MedicationAmount ?? await db.GetTable<MedicationSelection>()
                    .Where(candidate => candidate.Id == selectionId)
                    .Select(candidate => candidate.AmountPerTime)
                    .FirstOrDefaultAsync();

                var selection = await ReadSelection(SyncSnapshotReader.MedicineSelectionTable, selectionId.ToString("D"));

                await db.InsertAsync(new Medication
                {
                    Id = id, Timestamp = timestamp, HasPicture = 0, BabyId = babyId,
                    Time = time, Note = note,
                    MedicineSelectionId = selectionId,
                    Amount = amount
                });

                transaction = selection is null ? null : SyncTransactionBuilder.Medication(common, selection, amount);
                break;
            }

            case EntryTypeEnum.Vaccine:
            {
                var selection = await ReadSelection(SyncSnapshotReader.VaccineSelectionTable, model.VaccineSelectionId);

                // Every vaccine belongs to a doctor visit (DocVisitID is NOT NULL), so record one alongside it
                var doctorVisitId = Guid.NewGuid();

                await db.InsertAsync(new DoctorVisit
                {
                    Id = doctorVisitId, Timestamp = timestamp, HasPicture = 0, BabyId = babyId,
                    Time = time, Note = string.Empty, IsAppointment = 0
                });

                await db.InsertAsync(new Vaccine
                {
                    Id = id, Timestamp = timestamp, HasPicture = 0, BabyId = babyId,
                    Time = time, Note = note,
                    VaccineId = model.VaccineSelectionId ?? string.Empty,
                    DocVisitId = doctorVisitId
                });

                transaction = selection is null ? null : SyncTransactionBuilder.Vaccine(common, selection);
                break;
            }

            case EntryTypeEnum.Supplement:
            {
                var unit = string.IsNullOrWhiteSpace(model.SupplementUnit) ? "ml" : model.SupplementUnit.Trim();
                var selection = await ReadSelection(SyncSnapshotReader.FeedSelectionTable, model.SupplementSelectionId);

                await db.InsertAsync(new Supplement
                {
                    Id = id, Timestamp = timestamp, HasPicture = 0, BabyId = babyId,
                    Time = time, Note = note,
                    TypeId = model.SupplementSelectionId ?? string.Empty,
                    Amount = model.SupplementAmount ?? 0,
                    Unit = unit
                });

                transaction = selection is null
                    ? null
                    : SyncTransactionBuilder.OtherFeed(common, selection, model.SupplementAmount ?? 0, unit);
                break;
            }

            default:
                throw new InvalidOperationException($"Unsupported entry type {model.Type}.");
        }

        if (hasPicture)
        {
            await db.InsertAsync(new Picture
            {
                Id = pictureId!.Value, Timestamp = timestamp, ActivityId = id,
                Type = 0, // MediaTypePicture
                FileName = pictureFileName!,
                Thumbnail = picture!.Thumbnail
            });
        }

        if (!buildSyncEntry || transaction is null)
        {
            return null;
        }

        return new SyncEntry(
            SyncTransactionBuilder.Encode(transaction),
            hasPicture ? new SyncPictureFiles(pictureFileName!, picture!.Jpeg, picture.Thumbnail) : null);
    }

    public async Task<AddEntryViewModel?> GetEntryAsync(string babyName, EntryTypeEnum type, Guid id)
    {
        var babies = await sqLiteService.GetBabiesFromDb();

        var baby = babies.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, babyName, StringComparison.OrdinalIgnoreCase));

        if (baby is null)
        {
            return null;
        }

        IDbEntry? entry = type switch
        {
            EntryTypeEnum.Diaper => await sqLiteService.GetEntryAsync<Diaper>(id),
            EntryTypeEnum.Formula => await sqLiteService.GetEntryAsync<Formula>(id),
            EntryTypeEnum.Sleep => await sqLiteService.GetEntryAsync<Sleep>(id),
            EntryTypeEnum.Joy => await sqLiteService.GetEntryAsync<Joy>(id, row => new Joy
            {
                Id = row.Id, BabyId = row.BabyId, Timestamp = row.Timestamp, Time = row.Time, Note = row.Note
            }),
            EntryTypeEnum.Activity => await sqLiteService.GetEntryAsync<Activity>(id, row => new Activity
            {
                Id = row.Id, BabyId = row.BabyId, Timestamp = row.Timestamp, Time = row.Time, Note = row.Note,
                DescriptionId = row.DescriptionId, Duration = row.Duration
            }),
            EntryTypeEnum.Growth => await sqLiteService.GetEntryAsync<Growth>(id),
            EntryTypeEnum.Temperature => await sqLiteService.GetEntryAsync<Temperature>(id),
            EntryTypeEnum.Milestone => await sqLiteService.GetEntryAsync<Milestone>(id, row => new Milestone
            {
                Id = row.Id, BabyId = row.BabyId, Timestamp = row.Timestamp, Time = row.Time, Note = row.Note,
                MilestoneSelectionId = row.MilestoneSelectionId
            }),
            EntryTypeEnum.Medication => await sqLiteService.GetEntryAsync<Medication>(id),
            EntryTypeEnum.Vaccine => await sqLiteService.GetEntryAsync<Vaccine>(id),
            EntryTypeEnum.Supplement => await sqLiteService.GetEntryAsync<Supplement>(id),
            _ => null
        };

        // An entry of another baby is not found through this baby
        if (entry is null || entry.BabyId != baby.Id)
        {
            return null;
        }

        var model = new AddEntryViewModel
        {
            Id = id,
            Type = type,
            Time = entry.Time.ToDateTimeLocal(),
            Note = entry.Note
        };

        switch (entry)
        {
            case Diaper diaper:
                model.DiaperStatus = diaper.Status;
                break;

            case Formula formula:
                model.FormulaAmount = formula.Amount;
                break;

            case Sleep sleep:
                model.SleepMinutes = sleep.Duration;
                break;

            case Activity activity:
                model.ActivityDescriptionId = activity.DescriptionId;
                model.ActivityMinutes = activity.Duration;
                break;

            case Growth growth:
                // The app stores 0 for a measurement that was not filled in
                model.GrowthWeight = growth.Weight == 0 ? null : growth.Weight;
                model.GrowthLength = growth.Length == 0 ? null : growth.Length;
                model.GrowthHeadSize = growth.HeadSize == 0 ? null : growth.HeadSize;
                break;

            case Temperature temperature:
                model.TemperatureCelsius = temperature.Temp;
                break;

            case Milestone milestone:
                model.MilestoneSelectionId = milestone.MilestoneSelectionId;
                break;

            case Medication medication:
                model.MedicationSelectionId = medication.MedicineSelectionId;
                model.MedicationAmount = medication.Amount;
                break;

            case Vaccine vaccine:
                model.VaccineSelectionId = vaccine.VaccineId;
                break;

            case Supplement supplement:
                model.SupplementSelectionId = supplement.TypeId;
                model.SupplementAmount = supplement.Amount;
                model.SupplementUnit = supplement.Unit;
                break;
        }

        return model;
    }

    public async Task<AddEntryResult> UpdateEntryAsync(string userId, string babyName, Guid id, AddEntryViewModel model,
        ProcessedPicture? picture = null)
    {
        var syncEnabled = await syncService.IsEnabledAsync(userId);

        Guid? pictureId = null;
        string? pictureFileName = null;
        SyncEntry? syncEntry = null;
        var replacedFileNames = new List<string>();

        try
        {
            if (picture is not null && model.Type.SupportsPicture())
            {
                // A new picture of the app is named after its own id
                pictureId = Guid.NewGuid();
                pictureFileName = pictureId.Value.ToString("D");

                // The files go first, like when adding: a failure afterwards only leaves files behind, removed below
                await pictureService.SavePictureAsync(userId, pictureFileName, picture.Jpeg);
                await pictureService.SavePictureAsync(userId, $"{pictureFileName}{ThumbnailSuffix}", picture.Thumbnail);
            }

            await sqLiteService.AddEntryAsync(userId, async db =>
            {
                syncEntry = await UpdateEntryRowAsync(db, babyName, id, model, syncEnabled,
                    pictureId, pictureFileName, picture, replacedFileNames);
            });
        }
        catch
        {
            if (pictureFileName is not null)
            {
                pictureService.DeletePicture(userId, pictureFileName);
                pictureService.DeletePicture(userId, $"{pictureFileName}{ThumbnailSuffix}");
            }

            throw;
        }

        // The old photo is not used anymore now that the change is in the clone
        replacedFileNames.ForEach(fileName =>
        {
            pictureService.DeletePicture(userId, fileName);
            pictureService.DeletePicture(userId, $"{fileName}{ThumbnailSuffix}");
        });

        if (syncEntry is null)
        {
            return new AddEntryResult(new SyncResult(SyncStatus.Skipped));
        }

        // The change is in the clone now, so a problem with the Baby Tracker server is only reported
        return new AddEntryResult(await syncService.PublishAsync(userId, syncEntry));
    }

    /// <returns>The change as a transaction for the Baby Tracker server, or null when <paramref name="buildSyncEntry"/> is false.</returns>
    private static async Task<SyncEntry?> UpdateEntryRowAsync(LinqToDB.Data.DataConnection db, string babyName, Guid id,
        AddEntryViewModel model, bool buildSyncEntry, Guid? pictureId, string? pictureFileName, ProcessedPicture? newPicture,
        List<string> replacedFileNames)
    {
        var baby = await GetBabyAsync(db, babyName);

        var babyId = baby.Id;
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var time = model.Time.ToUnixTimestampFromLocal();
        var note = model.Note ?? string.Empty;

        // Without a new photo the transaction only refers to the current one
        SyncPictureSnapshot? picture = null;
        Picture? existingPicture = null;
        var hasNewPicture = newPicture is not null && pictureId is not null && pictureFileName is not null &&
            model.Type.SupportsPicture();

        // Only changed when there is a new photo
        int? newHasPicture = hasNewPicture ? 1 : null;

        if (model.Type.SupportsPicture())
        {
            existingPicture = await db.GetTable<Picture>()
                .Where(candidate => candidate.ActivityId == id)
                .Select(candidate => new Picture { Id = candidate.Id, FileName = candidate.FileName })
                .FirstOrDefaultAsync();

            if (hasNewPicture)
            {
                picture = new SyncPictureSnapshot(pictureId!.Value, pictureFileName!, IsNew: true);
            }
            else if (existingPicture is not null)
            {
                picture = new SyncPictureSnapshot(existingPicture.Id, existingPicture.FileName);
            }
        }

        var common = new SyncCommon(
            id,
            timestamp,
            time,
            note,
            TimeExtensions.GetCloneTimeZoneRawOffsetSeconds(),
            ToSyncBaby(baby),
            picture,
            IsNew: false);

        async Task<SyncSelectionSnapshot?> ReadSelection(string table, string? selectionId)
            => buildSyncEntry
                ? await SyncSnapshotReader.ReadSelectionAsync(db, table, selectionId ?? string.Empty)
                : null;

        JsonObject? transaction = null;
        var updatedRows = 0;

        switch (model.Type)
        {
            case EntryTypeEnum.Diaper:
            {
                var status = model.DiaperStatus;

                updatedRows = await db.GetTable<Diaper>()
                    .Where(entry => entry.Id == id && entry.BabyId == babyId)
                    .UpdateAsync(entry => new Diaper { Timestamp = timestamp, Time = time, Note = note, Status = status });

                transaction = SyncTransactionBuilder.Diaper(common, ToSyncDiaperStatus(status));
                break;
            }

            case EntryTypeEnum.Formula:
            {
                var amount = model.FormulaAmount ?? 0;

                updatedRows = await db.GetTable<Formula>()
                    .Where(entry => entry.Id == id && entry.BabyId == babyId)
                    .UpdateAsync(entry => new Formula { Timestamp = timestamp, Time = time, Note = note, Amount = amount });

                transaction = SyncTransactionBuilder.Formula(common, amount);
                break;
            }

            case EntryTypeEnum.Sleep:
            {
                var duration = model.SleepMinutes ?? 0;

                updatedRows = await db.GetTable<Sleep>()
                    .Where(entry => entry.Id == id && entry.BabyId == babyId)
                    .UpdateAsync(entry => new Sleep { Timestamp = timestamp, Time = time, Note = note, Duration = duration });

                transaction = SyncTransactionBuilder.Sleep(common, duration);
                break;
            }

            case EntryTypeEnum.Joy:
                updatedRows = await db.GetTable<Joy>()
                    .Where(entry => entry.Id == id && entry.BabyId == babyId)
                    .UpdateAsync(entry => new Joy
                    {
                        Timestamp = timestamp, Time = time, Note = note, HasPicture = newHasPicture ?? entry.HasPicture
                    });

                transaction = SyncTransactionBuilder.Joy(common);
                break;

            case EntryTypeEnum.Activity:
            {
                var descriptionId = model.ActivityDescriptionId ?? string.Empty;
                var duration = model.ActivityMinutes ?? 0;
                var description = await ReadSelection(SyncSnapshotReader.ActivityDescriptionTable, model.ActivityDescriptionId);

                updatedRows = await db.GetTable<Activity>()
                    .Where(entry => entry.Id == id && entry.BabyId == babyId)
                    .UpdateAsync(entry => new Activity
                    {
                        Timestamp = timestamp, Time = time, Note = note, DescriptionId = descriptionId, Duration = duration,
                        HasPicture = newHasPicture ?? entry.HasPicture
                    });

                transaction = description is null
                    ? null
                    : SyncTransactionBuilder.OtherActivity(common, description, duration);
                break;
            }

            case EntryTypeEnum.Growth:
            {
                var weight = model.GrowthWeight ?? 0;
                var length = model.GrowthLength ?? 0;
                var headSize = model.GrowthHeadSize ?? 0;

                updatedRows = await db.GetTable<Growth>()
                    .Where(entry => entry.Id == id && entry.BabyId == babyId)
                    .UpdateAsync(entry => new Growth
                    {
                        Timestamp = timestamp, Time = time, Note = note, Weight = weight, Length = length, HeadSize = headSize
                    });

                transaction = SyncTransactionBuilder.Growth(common, weight, length, headSize);
                break;
            }

            case EntryTypeEnum.Temperature:
            {
                var celsius = model.TemperatureCelsius ?? 0;

                updatedRows = await db.GetTable<Temperature>()
                    .Where(entry => entry.Id == id && entry.BabyId == babyId)
                    .UpdateAsync(entry => new Temperature { Timestamp = timestamp, Time = time, Note = note, Temp = celsius });

                transaction = SyncTransactionBuilder.Temperature(common, celsius);
                break;
            }

            case EntryTypeEnum.Milestone:
            {
                var selectionId = model.MilestoneSelectionId;
                var selection = await ReadSelection(SyncSnapshotReader.MilestoneSelectionTable, selectionId);

                updatedRows = await db.GetTable<Milestone>()
                    .Where(entry => entry.Id == id && entry.BabyId == babyId)
                    .UpdateAsync(entry => new Milestone
                    {
                        Timestamp = timestamp, Time = time, Note = note, MilestoneSelectionId = selectionId ?? string.Empty,
                        HasPicture = newHasPicture ?? entry.HasPicture
                    });

                transaction = selection is null ? null : SyncTransactionBuilder.Milestone(common, selection);
                break;
            }

            case EntryTypeEnum.Medication:
            {
                var selectionId = model.MedicationSelectionId ?? Guid.Empty;

                // Default to the dose that is set up for the medication
                var amount = model.MedicationAmount ?? await db.GetTable<MedicationSelection>()
                    .Where(candidate => candidate.Id == selectionId)
                    .Select(candidate => candidate.AmountPerTime)
                    .FirstOrDefaultAsync();

                var selection = await ReadSelection(SyncSnapshotReader.MedicineSelectionTable, selectionId.ToString("D"));

                updatedRows = await db.GetTable<Medication>()
                    .Where(entry => entry.Id == id && entry.BabyId == babyId)
                    .UpdateAsync(entry => new Medication
                    {
                        Timestamp = timestamp, Time = time, Note = note, MedicineSelectionId = selectionId, Amount = amount
                    });

                transaction = selection is null ? null : SyncTransactionBuilder.Medication(common, selection, amount);
                break;
            }

            case EntryTypeEnum.Vaccine:
            {
                var vaccineId = model.VaccineSelectionId ?? string.Empty;
                var selection = await ReadSelection(SyncSnapshotReader.VaccineSelectionTable, model.VaccineSelectionId);

                var doctorVisitId = await db.GetTable<Vaccine>()
                    .Where(entry => entry.Id == id && entry.BabyId == babyId)
                    .Select(entry => entry.DocVisitId)
                    .FirstOrDefaultAsync();

                updatedRows = await db.GetTable<Vaccine>()
                    .Where(entry => entry.Id == id && entry.BabyId == babyId)
                    .UpdateAsync(entry => new Vaccine
                    {
                        Timestamp = timestamp, Time = time, Note = note, VaccineId = vaccineId
                    });

                // The doctor visit that was recorded with the vaccine moves along with it
                if (updatedRows > 0)
                {
                    await db.GetTable<DoctorVisit>()
                        .Where(visit => visit.Id == doctorVisitId)
                        .UpdateAsync(visit => new DoctorVisit { Timestamp = timestamp, Time = time });
                }

                transaction = selection is null ? null : SyncTransactionBuilder.Vaccine(common, selection);
                break;
            }

            case EntryTypeEnum.Supplement:
            {
                var typeId = model.SupplementSelectionId ?? string.Empty;
                var amount = model.SupplementAmount ?? 0;
                var unit = string.IsNullOrWhiteSpace(model.SupplementUnit) ? "ml" : model.SupplementUnit.Trim();
                var selection = await ReadSelection(SyncSnapshotReader.FeedSelectionTable, model.SupplementSelectionId);

                updatedRows = await db.GetTable<Supplement>()
                    .Where(entry => entry.Id == id && entry.BabyId == babyId)
                    .UpdateAsync(entry => new Supplement
                    {
                        Timestamp = timestamp, Time = time, Note = note, TypeId = typeId, Amount = amount, Unit = unit
                    });

                transaction = selection is null ? null : SyncTransactionBuilder.OtherFeed(common, selection, amount, unit);
                break;
            }

            default:
                throw new InvalidOperationException($"Unsupported entry type {model.Type}.");
        }

        // Nothing is saved when the entry is not there: the transaction of the write is rolled back
        if (updatedRows == 0)
        {
            throw new KeyNotFoundException($"The {model.Type} entry {id} of baby '{babyName}' was not found in the data clone.");
        }

        // The new photo replaces the current one: a Picture row that points at the entry
        if (hasNewPicture)
        {
            await db.GetTable<Picture>()
                .Where(candidate => candidate.ActivityId == id)
                .DeleteAsync();

            await db.InsertAsync(new Picture
            {
                Id = pictureId!.Value, Timestamp = timestamp, ActivityId = id,
                Type = 0, // MediaTypePicture
                FileName = pictureFileName!,
                Thumbnail = newPicture!.Thumbnail
            });

            if (existingPicture is not null)
            {
                replacedFileNames.Add(existingPicture.FileName);
            }
        }

        if (!buildSyncEntry || transaction is null)
        {
            return null;
        }

        return new SyncEntry(
            SyncTransactionBuilder.Encode(transaction),
            hasNewPicture ? new SyncPictureFiles(pictureFileName!, newPicture!.Jpeg, newPicture.Thumbnail) : null,
            SyncOpCodes.Update);
    }

    private static async Task<Baby> GetBabyAsync(LinqToDB.Data.DataConnection db, string babyName)
    {
        var baby = await db.GetTable<Baby>()
            .Where(candidate => candidate.Name.ToLower() == babyName.ToLower())
            .FirstOrDefaultAsync();

        if (baby is null || baby.Id == Guid.Empty)
        {
            throw new KeyNotFoundException($"Baby '{babyName}' was not found in the data clone.");
        }

        return baby;
    }

    private static SyncBabySnapshot ToSyncBaby(Baby baby)
        => new(
            baby.Id,
            baby.Timestamp,
            baby.Name,
            baby.DOB,
            baby.DueDate,
            baby.Gender != 0,
            string.IsNullOrEmpty(baby.PictureFileName) ? null : baby.PictureFileName);

    internal static string ToSyncDiaperStatus(DiaperStatusEnum status)
        => status switch
        {
            DiaperStatusEnum.Wet => "DiaperStatusWet",
            DiaperStatusEnum.Dirty => "DiaperStatusPoopy",
            DiaperStatusEnum.Mixed => "DiaperStatusMixed",
            _ => throw new InvalidOperationException($"Unsupported diaper status {status}.")
        };
}
