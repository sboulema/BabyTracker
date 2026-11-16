using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace BabyTracker.Sync;

public sealed class BabyTrackerSyncException(string message, HttpStatusCode? statusCode = null) : Exception(message)
{
    public HttpStatusCode? StatusCode { get; } = statusCode;
}

public sealed record SyncDevice(string DeviceUUID, int LastSyncID);

/// <summary>
/// One logged-in session with the Baby Tracker server, see docs/baby-tracker-sync-api.md.
/// The session cookie is kept here and not in the HttpClient, so sessions of different users
/// never share cookies.
/// </summary>
public sealed class BabyTrackerSyncSession
{
    private const string UploadTransactionOpCode = "TransactionLogOpCodeInsert";

    private readonly HttpClient httpClient;
    private readonly Dictionary<string, string> cookies = new(StringComparer.Ordinal);

    private BabyTrackerSyncSession(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public int AccountId { get; private set; }

    /// <summary>Logs in as a device of the group of <paramref name="account"/>.</summary>
    public static async Task<BabyTrackerSyncSession> LoginAsync(
        HttpClient httpClient,
        SyncAccount account,
        CancellationToken cancellationToken = default)
    {
        var session = new BabyTrackerSyncSession(httpClient);

        var body = new JsonObject
        {
            ["EmailAddress"] = account.EmailAddress,
            ["Password"] = account.Password,
            ["Device"] = new JsonObject
            {
                ["DeviceName"] = account.DeviceName,
                ["DeviceOSInfo"] = "Website",
                ["DeviceUUID"] = account.DeviceId
            },
            ["AppInfo"] = new JsonObject
            {
                ["AccountType"] = 0,
                ["AppType"] = 0
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "session")
        {
            Content = JsonContent(body)
        };

        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw new BabyTrackerSyncException("The Baby Tracker server did not accept the email address and password.", response.StatusCode);
        }

        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new BabyTrackerSyncException($"Logging in to the Baby Tracker server failed (HTTP {(int)response.StatusCode}).", response.StatusCode);
        }

        session.StoreCookies(response);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        session.AccountId = ParseAccountId(responseBody);

        return session;
    }

    public async Task<IReadOnlyList<SyncDevice>> GetDevicesAsync(CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, "account/device");
        using var response = await httpClient.SendAsync(request, cancellationToken);

        EnsureStatus(response, HttpStatusCode.OK, "Getting the devices");

        var devices = JsonSerializer.Deserialize<List<SyncDevice>>(
            await response.Content.ReadAsStringAsync(cancellationToken),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return devices ?? [];
    }

    /// <summary>Uploads one insert transaction. The server answers 201.</summary>
    /// <param name="syncId">The id of the transaction in the log of this device.</param>
    /// <param name="transaction">Base64 of the JSON, see <see cref="SyncTransactionBuilder.Encode"/>.</param>
    public async Task UploadTransactionAsync(int syncId, string transaction, CancellationToken cancellationToken = default)
    {
        var body = new JsonObject
        {
            ["OPCode"] = UploadTransactionOpCode,
            ["SyncID"] = syncId,
            ["Transaction"] = transaction
        };

        using var request = CreateRequest(HttpMethod.Post, "account/transaction");
        request.Content = JsonContent(body);

        using var response = await httpClient.SendAsync(request, cancellationToken);

        EnsureStatus(response, HttpStatusCode.Created, "Uploading the entry");
    }

    /// <summary>Uploads one photo file. <paramref name="fileName"/> includes the extension.</summary>
    public async Task UploadPictureAsync(string fileName, byte[] jpeg, CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();

        var file = new ByteArrayContent(jpeg);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(file, "picture", fileName);

        using var request = CreateRequest(HttpMethod.Post, "account/picture");
        request.Content = content;

        using var response = await httpClient.SendAsync(request, cancellationToken);

        EnsureStatus(response, HttpStatusCode.Created, "Uploading the photo");
    }

    internal static int ParseAccountId(string responseBody)
    {
        try
        {
            using var document = JsonDocument.Parse(responseBody);

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (string.Equals(property.Name, "AccountID", StringComparison.OrdinalIgnoreCase) &&
                    property.Value.TryGetInt32(out var accountId))
                {
                    return accountId;
                }
            }
        }
        catch (JsonException)
        {
            // Handled below
        }

        throw new BabyTrackerSyncException("The Baby Tracker server did not return an account id.");
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativeUrl)
    {
        var request = new HttpRequestMessage(method, relativeUrl);

        if (cookies.Count > 0)
        {
            request.Headers.TryAddWithoutValidation(
                "Cookie",
                string.Join("; ", cookies.Select(cookie => $"{cookie.Key}={cookie.Value}")));
        }

        return request;
    }

    private void StoreCookies(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            return;
        }

        foreach (var setCookie in setCookies)
        {
            var pair = setCookie.Split(';', 2)[0];
            var separator = pair.IndexOf('=');

            if (separator <= 0)
            {
                continue;
            }

            cookies[pair[..separator].Trim()] = pair[(separator + 1)..].Trim();
        }
    }

    private static StringContent JsonContent(JsonObject body)
        => new(body.ToJsonString(), Encoding.UTF8, "application/json");

    private static void EnsureStatus(HttpResponseMessage response, HttpStatusCode expected, string action)
    {
        if (response.StatusCode == expected)
        {
            return;
        }

        var message = response.StatusCode == HttpStatusCode.Unauthorized
            ? $"{action} failed: the Baby Tracker session was not accepted."
            : $"{action} failed (HTTP {(int)response.StatusCode}).";

        throw new BabyTrackerSyncException(message, response.StatusCode);
    }
}
