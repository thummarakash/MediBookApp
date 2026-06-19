using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MediBook.Configuration;
using MediBook.Services.Auth;

namespace MediBook.Services.Firebase;

/// <summary>
/// Wraps the Firestore REST API. All public methods accept and return plain
/// dictionaries so callers control serialization without a code-generated SDK.
/// </summary>
public class FirestoreService
{
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(AppConfig.HttpTimeoutSeconds) };

    public static FirestoreService Instance { get; } = new();
    private FirestoreService() { }

    // ── Documents ────────────────────────────────────────────────────────────

    public async Task<JsonElement?> GetDocumentAsync(string collection, string documentId)
    {
        var url = $"{AppConfig.FirestoreBaseUrl}/{collection}/{documentId}";
        var request = await BuildRequestAsync(HttpMethod.Get, url);
        var response = await _http.SendAsync(request);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement.Clone();
    }

    public async Task SetDocumentAsync(string collection, string documentId, Dictionary<string, object> fields)
    {
        var url = $"{AppConfig.FirestoreBaseUrl}/{collection}/{documentId}";
        var body = BuildDocumentBody(fields);
        var request = await BuildRequestAsync(HttpMethod.Patch, url, body);
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task<string> AddDocumentAsync(string collection, Dictionary<string, object> fields)
    {
        var url = $"{AppConfig.FirestoreBaseUrl}/{collection}";
        var body = BuildDocumentBody(fields);
        var request = await BuildRequestAsync(HttpMethod.Post, url, body);
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json).RootElement;
        // Extract the auto-generated document ID from the returned name
        var name = doc.GetProperty("name").GetString() ?? "";
        return name.Split('/').Last();
    }

    public async Task UpdateDocumentAsync(string collection, string documentId, Dictionary<string, object> fields)
    {
        var fieldPaths = string.Join("&", fields.Keys.Select(k => $"updateMask.fieldPaths={Uri.EscapeDataString(k)}"));
        var url = $"{AppConfig.FirestoreBaseUrl}/{collection}/{documentId}?{fieldPaths}";
        var body = BuildDocumentBody(fields);
        var request = await BuildRequestAsync(HttpMethod.Patch, url, body);
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteDocumentAsync(string collection, string documentId)
    {
        var url = $"{AppConfig.FirestoreBaseUrl}/{collection}/{documentId}";
        var request = await BuildRequestAsync(HttpMethod.Delete, url);
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    public async Task<List<FirestoreDocument>> QueryAsync(
        string collection,
        string? whereField = null,
        object? whereValue = null,
        string? orderByField = null,
        bool descending = false,
        int? limit = null)
    {
        var url = $"{AppConfig.FirestoreBaseUrl}:runQuery";

        var query = BuildQuery(collection, whereField, whereValue, orderByField, descending, limit);
        var request = await BuildRequestAsync(HttpMethod.Post, url, JsonSerializer.Serialize(query));
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var array = JsonDocument.Parse(json).RootElement;
        var results = new List<FirestoreDocument>();

        foreach (var item in array.EnumerateArray())
        {
            if (!item.TryGetProperty("document", out var doc)) continue;
            var name = doc.GetProperty("name").GetString() ?? "";
            var id = name.Split('/').Last();
            var fields = doc.TryGetProperty("fields", out var f) ? f : default;
            results.Add(new FirestoreDocument { Id = id, Fields = fields });
        }

        return results;
    }

    public async Task<List<FirestoreDocument>> GetCollectionAsync(string collection, int? limit = null)
    {
        var urlBuilder = new StringBuilder($"{AppConfig.FirestoreBaseUrl}/{collection}?");
        if (limit.HasValue) urlBuilder.Append($"pageSize={limit.Value}");

        var request = await BuildRequestAsync(HttpMethod.Get, urlBuilder.ToString());
        var response = await _http.SendAsync(request);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return new List<FirestoreDocument>();
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var root = JsonDocument.Parse(json).RootElement;
        var results = new List<FirestoreDocument>();

        if (root.TryGetProperty("documents", out var documents))
        {
            foreach (var doc in documents.EnumerateArray())
            {
                var name = doc.GetProperty("name").GetString() ?? "";
                var id = name.Split('/').Last();
                var fields = doc.TryGetProperty("fields", out var f) ? f : default;
                results.Add(new FirestoreDocument { Id = id, Fields = fields });
            }
        }

        return results;
    }

    // ── Field readers ─────────────────────────────────────────────────────────

    public static string GetString(JsonElement fields, string name)
    {
        if (fields.ValueKind == JsonValueKind.Undefined) return string.Empty;
        if (fields.TryGetProperty(name, out var field) && field.TryGetProperty("stringValue", out var val))
            return val.GetString() ?? string.Empty;
        return string.Empty;
    }

    public static bool GetBool(JsonElement fields, string name, bool defaultValue = false)
    {
        if (fields.ValueKind == JsonValueKind.Undefined) return defaultValue;
        if (fields.TryGetProperty(name, out var field) && field.TryGetProperty("booleanValue", out var val))
            return val.GetBoolean();
        return defaultValue;
    }

    public static double GetDouble(JsonElement fields, string name, double defaultValue = 0)
    {
        if (fields.ValueKind == JsonValueKind.Undefined) return defaultValue;
        if (fields.TryGetProperty(name, out var field))
        {
            if (field.TryGetProperty("doubleValue", out var dv)) return dv.GetDouble();
            if (field.TryGetProperty("integerValue", out var iv) && long.TryParse(iv.GetString(), out var l)) return l;
        }
        return defaultValue;
    }

    public static int GetInt(JsonElement fields, string name, int defaultValue = 0)
        => (int)GetDouble(fields, name, defaultValue);

    public static DateTime GetDateTime(JsonElement fields, string name)
    {
        if (fields.ValueKind == JsonValueKind.Undefined) return DateTime.Now;
        if (fields.TryGetProperty(name, out var field) && field.TryGetProperty("timestampValue", out var val))
        {
            if (DateTime.TryParse(val.GetString(), out var dt))
                return dt.ToLocalTime();
        }
        return DateTime.Now;
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    private static async Task<HttpRequestMessage> BuildRequestAsync(HttpMethod method, string url, string? jsonBody = null)
    {
        var request = new HttpRequestMessage(method, url);
        var token = await SessionService.Instance.GetValidTokenAsync();
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (jsonBody != null)
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        return request;
    }

    private static string BuildDocumentBody(Dictionary<string, object> fields)
    {
        var firestoreFields = new Dictionary<string, object>();
        foreach (var kvp in fields)
            firestoreFields[kvp.Key] = ToFirestoreValue(kvp.Value);

        return JsonSerializer.Serialize(new { fields = firestoreFields });
    }

    private static object ToFirestoreValue(object? value)
    {
        return value switch
        {
            null => new { nullValue = "NULL_VALUE" },
            string s => new { stringValue = s },
            bool b => new { booleanValue = b },
            int i => new { integerValue = i.ToString() },
            long l => new { integerValue = l.ToString() },
            double d => new { doubleValue = d },
            float f => new { doubleValue = (double)f },
            DateTime dt => new { timestampValue = dt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ") },
            _ => new { stringValue = value.ToString() ?? string.Empty }
        };
    }

    private static object BuildQuery(
        string collection,
        string? whereField,
        object? whereValue,
        string? orderByField,
        bool descending,
        int? limit)
    {
        var collectionSelector = new { from = new[] { new { collectionId = collection } } };

        object? where = null;
        if (whereField != null && whereValue != null)
        {
            where = new
            {
                fieldFilter = new
                {
                    field = new { fieldPath = whereField },
                    op = "EQUAL",
                    value = ToFirestoreValue(whereValue)
                }
            };
        }

        var orderBy = orderByField != null
            ? new[] { new { field = new { fieldPath = orderByField }, direction = descending ? "DESCENDING" : "ASCENDING" } }
            : null;

        return new
        {
            structuredQuery = new
            {
                from = collectionSelector.from,
                where,
                orderBy,
                limit
            }
        };
    }
}

public class FirestoreDocument
{
    public string Id { get; set; } = string.Empty;
    public JsonElement Fields { get; set; }
}
