using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BabyTracker.Constants;
using BabyTracker.Extensions;
using BabyTracker.Models.Database;
using BabyTracker.Sync;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;

namespace BabyTracker.Services;

/// <param name="Deleted">False when the clone has no entry of this type with this id.</param>
/// <param name="Sync">What happened with telling the Baby Tracker server about the deletion.</param>
public sealed record DeleteEntryResult(bool Deleted, SyncResult Sync);

public interface IDeleteEntryService
{
    /// <param name="userId">The active user id: the owner of the data clone to delete from.</param>
    Task<DeleteEntryResult> DeleteEntryAsync(string userId, EntryTypeEnum type, Guid id);
}

public class DeleteEntryService(
    ISqLiteService sqLiteService,
    IPictureService pictureService,
    ISyncService syncService) : IDeleteEntryService
{
    private const string ThumbnailSuffix = "__thumbnail";

    public async Task<DeleteEntryResult> DeleteEntryAsync(string userId, EntryTypeEnum type, Guid id)
    {
        var syncEnabled = await syncService.IsEnabledAsync(userId);

        var deleted = false;
        SyncEntry? syncEntry = null;
        var pictureFileNames = new List<string>();

        await sqLiteService.AddEntryAsync(userId, async db =>
        {
            // The transaction embeds the entry, so it is read before the rows go
            if (syncEnabled)
            {
                syncEntry = await SyncDeleteEntryBuilder.BuildAsync(db, type, id);
            }

            // Vaccines are the only entries that point at another row that has to go with them
            var doctorVisitId = type == EntryTypeEnum.Vaccine
                ? await db.GetTable<Vaccine>()
                    .Where(vaccine => vaccine.Id == id)
                    .Select(vaccine => vaccine.DocVisitId)
                    .FirstOrDefaultAsync()
                : Guid.Empty;

            deleted = await DeleteRowAsync(db, type, id) > 0;

            if (!deleted)
            {
                return;
            }

            if (type.SupportsPicture())
            {
                var pictures = await db.GetTable<Picture>()
                    .Where(picture => picture.ActivityId == id)
                    .Select(picture => picture.FileName)
                    .ToListAsync();

                pictureFileNames.AddRange(pictures);

                await db.GetTable<Picture>()
                    .Where(picture => picture.ActivityId == id)
                    .DeleteAsync();
            }

            if (doctorVisitId != Guid.Empty)
            {
                await DeleteOrphanedDoctorVisitAsync(db, doctorVisitId);
            }
        });

        // The files go last: the rows are committed by now, so a failure here only leaves files behind
        // instead of a Picture row without a file.
        pictureFileNames
            .SelectMany(fileName => new[] { fileName, $"{fileName}{ThumbnailSuffix}" })
            .ToList()
            .ForEach(fileName => pictureService.DeletePicture(userId, fileName));

        if (!deleted)
        {
            return new DeleteEntryResult(false, new SyncResult(SyncStatus.Skipped));
        }

        if (!syncEnabled)
        {
            return new DeleteEntryResult(true, new SyncResult(SyncStatus.Skipped));
        }

        if (syncEntry is null)
        {
            return new DeleteEntryResult(true,
                new SyncResult(SyncStatus.Failed, "The entry could not be converted for the Baby Tracker server."));
        }

        // The entry is gone from the clone now, so a problem with the Baby Tracker server is only reported
        return new DeleteEntryResult(true, await syncService.PublishAsync(userId, syncEntry));
    }

    private static Task<int> DeleteRowAsync(DataConnection db, EntryTypeEnum type, Guid id)
        => type switch
        {
            EntryTypeEnum.Diaper => db.GetTable<Diaper>().Where(entry => entry.Id == id).DeleteAsync(),
            EntryTypeEnum.Formula => db.GetTable<Formula>().Where(entry => entry.Id == id).DeleteAsync(),
            EntryTypeEnum.Sleep => db.GetTable<Sleep>().Where(entry => entry.Id == id).DeleteAsync(),
            EntryTypeEnum.Joy => db.GetTable<Joy>().Where(entry => entry.Id == id).DeleteAsync(),
            EntryTypeEnum.Activity => db.GetTable<Activity>().Where(entry => entry.Id == id).DeleteAsync(),
            EntryTypeEnum.Growth => db.GetTable<Growth>().Where(entry => entry.Id == id).DeleteAsync(),
            EntryTypeEnum.Temperature => db.GetTable<Temperature>().Where(entry => entry.Id == id).DeleteAsync(),
            EntryTypeEnum.Milestone => db.GetTable<Milestone>().Where(entry => entry.Id == id).DeleteAsync(),
            EntryTypeEnum.Medication => db.GetTable<Medication>().Where(entry => entry.Id == id).DeleteAsync(),
            EntryTypeEnum.Vaccine => db.GetTable<Vaccine>().Where(entry => entry.Id == id).DeleteAsync(),
            EntryTypeEnum.Supplement => db.GetTable<Supplement>().Where(entry => entry.Id == id).DeleteAsync(),
            _ => throw new InvalidOperationException($"Unsupported entry type {type}.")
        };

    /// <summary>
    /// Adding a vaccine records a doctor visit next to it. Remove that visit with its last vaccine,
    /// unless it holds something of its own (a note) or other vaccines still belong to it.
    /// </summary>
    private static async Task DeleteOrphanedDoctorVisitAsync(DataConnection db, Guid doctorVisitId)
    {
        var otherVaccines = await db.GetTable<Vaccine>()
            .CountAsync(vaccine => vaccine.DocVisitId == doctorVisitId);

        if (otherVaccines > 0)
        {
            return;
        }

        await db.GetTable<DoctorVisit>()
            .Where(visit => visit.Id == doctorVisitId && visit.Note == string.Empty)
            .DeleteAsync();
    }
}
