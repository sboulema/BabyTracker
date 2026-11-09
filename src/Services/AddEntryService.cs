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
using LinqToDB;
using LinqToDB.Async;

namespace BabyTracker.Services;

/// <param name="Sync">What happened with sending the entry to the Baby Tracker server.</param>
public sealed record AddEntryResult(SyncResult Sync);

public interface IAddEntryService
{
    /// <param name="userId">The active user id: the owner of the data clone to write to.</param>
    /// <param name="picture">The processed photo; required for a Picture entry.</param>
    /// <exception cref="KeyNotFoundException">There is no baby with this name in the data clone.</exception>
    Task<AddEntryResult> AddEntryAsync(string userId, string babyName, AddEntryViewModel model, ProcessedPicture? picture = null);
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
            if (model.Type == EntryTypeEnum.Picture)
            {
                ArgumentNullException.ThrowIfNull(picture);

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
        var baby = await db.GetTable<Baby>()
            .Where(candidate => candidate.Name == babyName)
            .FirstOrDefaultAsync();

        if (baby is null || baby.Id == Guid.Empty)
        {
            throw new KeyNotFoundException($"Baby '{babyName}' was not found in the data clone.");
        }

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
            new SyncBabySnapshot(
                baby.Id,
                baby.Timestamp,
                baby.Name,
                baby.DOB,
                baby.DueDate,
                baby.Gender != 0,
                string.IsNullOrEmpty(baby.PictureFileName) ? null : baby.PictureFileName));

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
                    Id = id, Timestamp = timestamp, HasPicture = 0, BabyId = babyId,
                    Time = time, Note = note
                });

                transaction = SyncTransactionBuilder.Joy(common);
                break;

            case EntryTypeEnum.Activity:
            {
                var description = await ReadSelection(SyncSnapshotReader.ActivityDescriptionTable, model.ActivityDescriptionId);

                await db.InsertAsync(new Activity
                {
                    Id = id, Timestamp = timestamp, HasPicture = 0, BabyId = babyId,
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
                    Id = id, Timestamp = timestamp, HasPicture = 0, BabyId = babyId,
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

            case EntryTypeEnum.Picture:
            {
                ArgumentNullException.ThrowIfNull(pictureId);
                ArgumentNullException.ThrowIfNull(pictureFileName);
                ArgumentNullException.ThrowIfNull(picture);

                // A photo is a Joy that has a Picture
                await db.InsertAsync(new Joy
                {
                    Id = id, Timestamp = timestamp, HasPicture = 1, BabyId = babyId,
                    Time = time, Note = note
                });

                await db.InsertAsync(new Picture
                {
                    Id = pictureId.Value, Timestamp = timestamp, ActivityId = id,
                    Type = 0, // MediaTypePicture
                    FileName = pictureFileName,
                    Thumbnail = picture.Thumbnail
                });

                transaction = SyncTransactionBuilder.Joy(
                    common with { Picture = new SyncPictureSnapshot(pictureId.Value, pictureFileName) });
                break;
            }

            default:
                throw new InvalidOperationException($"Unsupported entry type {model.Type}.");
        }

        if (!buildSyncEntry || transaction is null)
        {
            return null;
        }

        return new SyncEntry(
            SyncTransactionBuilder.Encode(transaction),
            model.Type == EntryTypeEnum.Picture && picture is not null && pictureFileName is not null
                ? new SyncPictureFiles(pictureFileName, picture.Jpeg, picture.Thumbnail)
                : null);
    }

    private static string ToSyncDiaperStatus(DiaperStatusEnum status)
        => status switch
        {
            DiaperStatusEnum.Wet => "DiaperStatusWet",
            DiaperStatusEnum.Dirty => "DiaperStatusPoopy",
            DiaperStatusEnum.Mixed => "DiaperStatusMixed",
            _ => throw new InvalidOperationException($"Unsupported diaper status {status}.")
        };
}
