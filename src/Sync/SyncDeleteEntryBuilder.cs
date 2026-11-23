using System;
using System.Linq;
using System.Threading.Tasks;
using BabyTracker.Constants;
using BabyTracker.Extensions;
using BabyTracker.Models.Database;
using BabyTracker.Services;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;

namespace BabyTracker.Sync;

/// <summary>
/// Builds the transaction that tells the Baby Tracker server an entry was deleted. The payload is the
/// entry as it is in the clone, as an existing object (<c>newFlage</c> false), sent with the delete opcode.
/// Call it before the rows are deleted from the clone.
/// </summary>
public static class SyncDeleteEntryBuilder
{
    /// <returns>
    /// The delete transaction, or null when the entry or something the transaction embeds
    /// (the baby, a selection) is not in the clone.
    /// </returns>
    public static async Task<SyncEntry?> BuildAsync(DataConnection db, EntryTypeEnum type, Guid id)
    {
        try
        {
            var transaction = await BuildTransactionAsync(db, type, id);

            if (transaction is null)
            {
                return null;
            }

            // The photo of the entry goes with it, as a transaction of its own
            var picture = await ReadPictureAsync(db, id);

            return new SyncEntry(
                SyncTransactionBuilder.Encode(transaction),
                null,
                SyncOpCodes.Delete,
                picture is null ? null : SyncTransactionBuilder.Encode(SyncTransactionBuilder.PictureDelete(id, picture)));
        }
        catch (InvalidOperationException)
        {
            // A selection that no longer exists
            return null;
        }
    }

    private static async Task<System.Text.Json.Nodes.JsonObject?> BuildTransactionAsync(DataConnection db, EntryTypeEnum type, Guid id)
    {
        async Task<SyncSelectionSnapshot> Selection(string table, string? selectionId)
            => await SyncSnapshotReader.ReadSelectionAsync(db, table, selectionId ?? string.Empty);

        switch (type)
        {
            case EntryTypeEnum.Diaper:
            {
                var row = await db.GetTable<Diaper>().Where(entry => entry.Id == id).FirstOrDefaultAsync();

                return row is null
                    ? null
                    : SyncTransactionBuilder.Diaper(
                        await CommonAsync(db, row), AddEntryService.ToSyncDiaperStatus(row.Status));
            }

            case EntryTypeEnum.Formula:
            {
                var row = await db.GetTable<Formula>().Where(entry => entry.Id == id).FirstOrDefaultAsync();

                return row is null
                    ? null
                    : SyncTransactionBuilder.Formula(await CommonAsync(db, row), row.Amount);
            }

            case EntryTypeEnum.Sleep:
            {
                var row = await db.GetTable<Sleep>().Where(entry => entry.Id == id).FirstOrDefaultAsync();

                return row is null
                    ? null
                    : SyncTransactionBuilder.Sleep(await CommonAsync(db, row), row.Duration);
            }

            case EntryTypeEnum.Joy:
            {
                var row = await db.GetTable<Joy>().Where(entry => entry.Id == id).FirstOrDefaultAsync();

                return row is null
                    ? null
                    : SyncTransactionBuilder.Joy(await CommonAsync(db, row));
            }

            case EntryTypeEnum.Activity:
            {
                var row = await db.GetTable<Activity>().Where(entry => entry.Id == id).FirstOrDefaultAsync();

                return row is null
                    ? null
                    : SyncTransactionBuilder.OtherActivity(
                        await CommonAsync(db, row),
                        await Selection(SyncSnapshotReader.ActivityDescriptionTable, row.DescriptionId),
                        row.Duration);
            }

            case EntryTypeEnum.Growth:
            {
                var row = await db.GetTable<Growth>().Where(entry => entry.Id == id).FirstOrDefaultAsync();

                // A measurement that was not entered is stored as 0 and is left out of the transaction, like on adding
                return row is null
                    ? null
                    : SyncTransactionBuilder.Growth(
                        await CommonAsync(db, row),
                        row.Weight > 0 ? row.Weight : null,
                        row.Length > 0 ? row.Length : null,
                        row.HeadSize > 0 ? row.HeadSize : null);
            }

            case EntryTypeEnum.Temperature:
            {
                var row = await db.GetTable<Temperature>().Where(entry => entry.Id == id).FirstOrDefaultAsync();

                return row is null
                    ? null
                    : SyncTransactionBuilder.Temperature(await CommonAsync(db, row), row.Temp);
            }

            case EntryTypeEnum.Milestone:
            {
                var row = await db.GetTable<Milestone>().Where(entry => entry.Id == id).FirstOrDefaultAsync();

                return row is null
                    ? null
                    : SyncTransactionBuilder.Milestone(
                        await CommonAsync(db, row),
                        await Selection(SyncSnapshotReader.MilestoneSelectionTable, row.MilestoneSelectionId));
            }

            case EntryTypeEnum.Medication:
            {
                var row = await db.GetTable<Medication>().Where(entry => entry.Id == id).FirstOrDefaultAsync();

                return row is null
                    ? null
                    : SyncTransactionBuilder.Medication(
                        await CommonAsync(db, row),
                        await Selection(SyncSnapshotReader.MedicineSelectionTable, row.MedicineSelectionId.ToString("D")),
                        row.Amount);
            }

            case EntryTypeEnum.Vaccine:
            {
                var row = await db.GetTable<Vaccine>().Where(entry => entry.Id == id).FirstOrDefaultAsync();

                return row is null
                    ? null
                    : SyncTransactionBuilder.Vaccine(
                        await CommonAsync(db, row),
                        await Selection(SyncSnapshotReader.VaccineSelectionTable, row.VaccineId));
            }

            case EntryTypeEnum.Supplement:
            {
                var row = await db.GetTable<Supplement>().Where(entry => entry.Id == id).FirstOrDefaultAsync();

                return row is null
                    ? null
                    : SyncTransactionBuilder.OtherFeed(
                        await CommonAsync(db, row),
                        await Selection(SyncSnapshotReader.FeedSelectionTable, row.TypeId),
                        row.Amount,
                        row.Unit);
            }

            default:
                throw new InvalidOperationException($"Unsupported entry type {type}.");
        }
    }

    private static async Task<SyncPictureSnapshot?> ReadPictureAsync(DataConnection db, Guid entryId)
    {
        var picture = await db.GetTable<Picture>()
            .Where(candidate => candidate.ActivityId == entryId)
            .Select(candidate => new { candidate.Id, candidate.FileName, candidate.Timestamp })
            .FirstOrDefaultAsync();

        return picture is null
            ? null
            : new SyncPictureSnapshot(picture.Id, picture.FileName, picture.Timestamp);
    }

    /// <summary>What every entry has in common, as an existing object.</summary>
    /// <exception cref="InvalidOperationException">The baby of the entry is not in the clone.</exception>
    private static async Task<SyncCommon> CommonAsync(DataConnection db, IDbEntry entry)
    {
        var baby = await db.GetTable<Baby>()
            .Where(candidate => candidate.Id == entry.BabyId)
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException($"The baby of entry {entry.Id} was not found in the data clone.");

        var picture = await ReadPictureAsync(db, entry.Id);

        return new SyncCommon(
            entry.Id,
            entry.Timestamp,
            entry.Time,
            entry.Note ?? string.Empty,
            TimeExtensions.GetCloneTimeZoneRawOffsetSeconds(),
            new SyncBabySnapshot(
                baby.Id,
                baby.Timestamp,
                baby.Name,
                baby.DOB,
                baby.DueDate,
                baby.Gender != 0,
                string.IsNullOrEmpty(baby.PictureFileName) ? null : baby.PictureFileName),
            picture,
            IsNew: false);
    }
}
