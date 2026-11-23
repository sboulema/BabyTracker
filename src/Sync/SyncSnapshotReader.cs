using System;
using System.Threading.Tasks;
using LinqToDB.Data;

namespace BabyTracker.Sync;

/// <summary>
/// Reads a selection (milestone, medication, ...) with all its columns, because the transaction of an
/// entry embeds the whole selection. The Models.Database classes only map the columns the website shows.
/// </summary>
public static class SyncSnapshotReader
{
    public const string ActivityDescriptionTable = "OtherActivityDesc";
    public const string MilestoneSelectionTable = "MilestoneSelection";
    public const string MedicineSelectionTable = "MedicineSelection";
    public const string VaccineSelectionTable = "VaccineSelection";
    public const string FeedSelectionTable = "OtherFeedSelection";

    private sealed class SelectionRow
    {
        public string Id { get; set; } = string.Empty;
        public double Timestamp { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? ForAge { get; set; }
        public string? BabyId { get; set; }
        public int? IsBottle { get; set; }
        public int? IsPrescription { get; set; }
        public double? AmountPerTime { get; set; }
        public string? Unit { get; set; }
        public int? Interval { get; set; }
    }

    /// <exception cref="InvalidOperationException">The selection does not exist.</exception>
    public static async Task<SyncSelectionSnapshot> ReadSelectionAsync(DataConnection db, string table, string id)
    {
        // The id is written as a literal, which is safe because it is parsed as a Guid first
        if (!Guid.TryParse(id, out var guid))
        {
            throw new InvalidOperationException($"'{id}' is not a valid selection id.");
        }

        var columns = table switch
        {
            ActivityDescriptionTable => "ID AS Id, Timestamp, Name, Description",
            MilestoneSelectionTable => "ID AS Id, Timestamp, Name, Description, ForAge, BabyID AS BabyId",
            MedicineSelectionTable => "ID AS Id, Timestamp, Name, Description, IsPrescription, AmountPerTime, Unit, Interval",
            VaccineSelectionTable => "ID AS Id, Timestamp, Name, Description, ForAge",
            FeedSelectionTable => "ID AS Id, Timestamp, Name, Description, IsBottle",
            _ => throw new ArgumentOutOfRangeException(nameof(table), table, "Unknown selection table.")
        };

        var rows = await db.QueryToListAsync<SelectionRow>(
            $"SELECT {columns} FROM {table} WHERE ID = '{guid:D}' COLLATE NOCASE");

        if (rows.Count == 0)
        {
            throw new InvalidOperationException($"The selection {id} does not exist in {table}.");
        }

        var row = rows[0];

        return new SyncSelectionSnapshot(
            row.Id,
            (long)row.Timestamp,
            row.Name,
            row.Description,
            row.ForAge,
            row.BabyId,
            row.IsBottle is null ? null : row.IsBottle != 0,
            row.IsPrescription is null ? null : row.IsPrescription != 0,
            row.AmountPerTime,
            row.Interval,
            row.Unit);
    }
}
