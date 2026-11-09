using System;
using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;

namespace BabyTracker.Sync;

/// <summary>
/// Builds the JSON of a transaction the way the Android app (version 6.04) does: Gson with a
/// runtime type adapter that adds a <c>BCObjectType</c> property, all fields of the class and
/// its parents, no nulls, and dates as <c>yyyy-MM-dd HH:mm:ss Z</c> in GMT.
/// The field names are the names of the Java fields.
/// </summary>
public static class SyncTransactionBuilder
{
    public static string FormatDate(long unixSeconds)
        => DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime
            .ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " +0000";

    /// <summary>Base64 (standard alphabet, padded, no line breaks) of the UTF-8 JSON.</summary>
    public static string Encode(JsonObject transaction)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(transaction.ToJsonString()));

    public static JsonObject Diaper(SyncCommon common, string statusName, string amountName = "DiaperAmountNoset")
    {
        var json = Activity("Diaper", common);
        json["amount"] = amountName;
        json["status"] = statusName;

        return json;
    }

    public static JsonObject Formula(SyncCommon common, double amountMl)
    {
        var json = Activity("Formula", common);
        json["amount"] = Measure(amountMl);

        return json;
    }

    public static JsonObject Sleep(SyncCommon common, int durationMinutes)
    {
        var json = Activity("Sleep", common);
        json["duration"] = durationMinutes;

        return json;
    }

    public static JsonObject Joy(SyncCommon common) => Activity("Joy", common);

    public static JsonObject OtherActivity(SyncCommon common, SyncSelectionSnapshot description, int durationMinutes)
    {
        var json = Activity("OtherActivity", common);
        json["desc"] = Selection(description);
        json["duration"] = durationMinutes;

        return json;
    }

    public static JsonObject Growth(SyncCommon common, double? weightKg, double? lengthCm, double? headCm)
    {
        var json = Activity("Growth", common);
        json["deleted"] = false;
        json["queryIndex"] = 0;

        if (headCm is not null)
        {
            json["head"] = Measure(headCm.Value);
        }

        if (lengthCm is not null)
        {
            json["length"] = Measure(lengthCm.Value);
        }

        if (weightKg is not null)
        {
            json["weight"] = Measure(weightKg.Value);
        }

        return json;
    }

    public static JsonObject Temperature(SyncCommon common, double celsius)
    {
        var json = Activity("Temperature", common);
        json["temperature"] = Measure(celsius);

        return json;
    }

    public static JsonObject Milestone(SyncCommon common, SyncSelectionSnapshot selection)
    {
        var json = Activity("Milestone", common);
        json["hasNewType"] = false;
        json["milestoneType"] = Selection(selection);

        return json;
    }

    public static JsonObject Medication(SyncCommon common, SyncSelectionSnapshot selection, double amount)
    {
        var json = Activity("Medication", common);
        json["amount"] = (float)amount;
        json["medicationSelection"] = Selection(selection);

        return json;
    }

    public static JsonObject Vaccine(SyncCommon common, SyncSelectionSnapshot selection)
    {
        var json = Activity("Vaccine", common);
        json["vaccineType"] = Selection(selection);

        return json;
    }

    public static JsonObject OtherFeed(SyncCommon common, SyncSelectionSnapshot feedType, double amount, string unit)
    {
        var json = Activity("OtherFeed", common);
        json["amount"] = Measure(amount);
        json["feedType"] = Selection(feedType);
        json["unit"] = unit;

        return json;
    }

    /// <summary>The fields of BCObject, Activity and BabyActivity, which every entry has.</summary>
    private static JsonObject Activity(string objectType, SyncCommon common)
    {
        var json = new JsonObject
        {
            ["BCObjectType"] = objectType,
            // Serialized while the object is still new, which is why this is true for an insert
            ["newFlage"] = true,
            ["objectID"] = common.Id.ToString("D"),
            ["timestamp"] = FormatDate(common.Timestamp),
            ["note"] = common.Note,
            ["pictureLoaded"] = common.Picture is not null,
            ["pictureNote"] = new JsonArray(),
            ["time"] = FormatDate(common.Time),
            ["timezone"] = common.TimezoneOffsetSeconds,
            ["baby"] = Baby(common.Baby)
        };

        if (common.Picture is { } picture)
        {
            json["pictureNote"] = new JsonArray(Picture(common.Id, picture));
        }

        return json;
    }

    private static JsonObject Baby(SyncBabySnapshot baby)
    {
        var json = new JsonObject
        {
            ["newFlage"] = false,
            ["objectID"] = baby.Id.ToString("D"),
            ["timestamp"] = FormatDate(baby.Timestamp),
            ["dob"] = FormatDate(baby.Dob),
            ["dueDay"] = FormatDate(baby.DueDay),
            ["gender"] = baby.Gender,
            ["name"] = baby.Name
        };

        if (!string.IsNullOrEmpty(baby.PictureName))
        {
            json["pictureName"] = baby.PictureName;
        }

        return json;
    }

    private static JsonObject Picture(Guid activityId, SyncPictureSnapshot picture)
        => new()
        {
            ["newFlage"] = true,
            ["objectID"] = picture.Id.ToString("D"),
            ["activityID"] = activityId.ToString("D"),
            ["attached"] = true,
            ["deleted"] = false,
            ["fileName"] = picture.FileName,
            ["mediaType"] = "MediaTypePicture"
        };

    private static JsonObject Measure(double value)
        => new()
        {
            ["englishMeasure"] = false,
            ["value"] = (float)value
        };

    /// <summary>The fields of EditableSelection and of its subclasses.</summary>
    private static JsonObject Selection(SyncSelectionSnapshot selection)
    {
        var json = new JsonObject
        {
            ["newFlage"] = false,
            ["objectID"] = selection.Id,
            ["timestamp"] = FormatDate(selection.Timestamp),
            ["name"] = selection.Name
        };

        if (!string.IsNullOrEmpty(selection.Description))
        {
            json["desc"] = selection.Description;
        }

        if (selection.BabyId is not null)
        {
            json["babyID"] = selection.BabyId;
        }

        if (selection.ForAge is not null)
        {
            json["forAge"] = selection.ForAge.Value;
        }

        if (selection.IsBottle is not null)
        {
            json["bottle"] = selection.IsBottle.Value;
        }

        if (selection.IsPrescription is not null)
        {
            json["prescription"] = selection.IsPrescription.Value;
        }

        if (selection.AmountPerTime is not null)
        {
            json["amountPerTime"] = (float)selection.AmountPerTime.Value;
        }

        if (selection.Interval is not null)
        {
            json["interval"] = selection.Interval.Value;
        }

        if (!string.IsNullOrEmpty(selection.Unit))
        {
            json["unit"] = selection.Unit;
        }

        return json;
    }
}
