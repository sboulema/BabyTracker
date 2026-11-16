using BabyTracker.Constants;

namespace BabyTracker.Extensions;

public static class EntryTypeExtensions
{
    /// <summary>The entry types that can have a photo, like in the app.</summary>
    public static bool SupportsPicture(this EntryTypeEnum type)
        => type is EntryTypeEnum.Joy or EntryTypeEnum.Activity or EntryTypeEnum.Milestone;
}
