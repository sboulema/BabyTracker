using System;

namespace BabyTracker.Sync;

/// <summary>
/// The Baby Tracker sync account (the "group") of a user, plus the identity of this website
/// as a device in that group. Stored encrypted, see <see cref="ISyncAccountStore"/>.
/// </summary>
public sealed class SyncAccount
{
    public bool Enabled { get; set; }

    public string EmailAddress { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    /// <summary>The id of this website as a device. Lowercase, like the ids the app generates.</summary>
    public string DeviceId { get; set; } = Guid.NewGuid().ToString("D");

    public string DeviceName { get; set; } = "BabyTracker website";

    /// <summary>The account id the server reported at the first login, to detect a changed account.</summary>
    public int? AccountId { get; set; }

    /// <summary>
    /// The last <c>SyncID</c> that was uploaded for this device. Every device has its own log, and
    /// the other devices pull it with the last id they have merged, so this only ever increases.
    /// </summary>
    public int LastSyncId { get; set; }
}
