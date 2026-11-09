using System;

namespace BabyTracker.Sync;

/// <summary>A <c>Baby</c> as it is embedded in the transaction of every entry.</summary>
public sealed record SyncBabySnapshot(
    Guid Id,
    long Timestamp,
    string Name,
    long Dob,
    long DueDay,
    bool Gender,
    string? PictureName);

/// <summary>
/// A selection (milestone, medication, vaccine, feed type or activity description) as it is
/// embedded in a transaction. Only the fields of the kind of selection that is used are filled.
/// </summary>
public sealed record SyncSelectionSnapshot(
    string Id,
    long Timestamp,
    string Name,
    string? Description,
    int? ForAge = null,
    string? BabyId = null,
    bool? IsBottle = null,
    bool? IsPrescription = null,
    double? AmountPerTime = null,
    int? Interval = null,
    string? Unit = null);

/// <summary>A photo that belongs to an entry. The file name is the id of the picture, without extension.</summary>
public sealed record SyncPictureSnapshot(Guid Id, string FileName);

/// <summary>What every entry has in common.</summary>
/// <param name="Id">The id of the entry.</param>
/// <param name="Timestamp">When the entry was saved, in Unix seconds.</param>
/// <param name="Time">When the entry happened, in Unix seconds.</param>
/// <param name="TimezoneOffsetSeconds">The raw offset of the time zone of the clone, without daylight saving time.</param>
public sealed record SyncCommon(
    Guid Id,
    long Timestamp,
    long Time,
    string Note,
    int TimezoneOffsetSeconds,
    SyncBabySnapshot Baby,
    SyncPictureSnapshot? Picture = null);

/// <summary>An entry that was added to the clone, ready to be sent to the Baby Tracker server.</summary>
/// <param name="Transaction">Base64 of the JSON of the entry, as it goes in <c>TransactionItem.Transaction</c>.</param>
/// <param name="Picture">The photo and thumbnail files that belong to the entry, if any.</param>
public sealed record SyncEntry(string Transaction, SyncPictureFiles? Picture = null);

public sealed record SyncPictureFiles(string FileName, byte[] Jpeg, byte[] Thumbnail);
