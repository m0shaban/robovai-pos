using SmartPOS.Core.Licensing;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    app.Urls.Add($"http://0.0.0.0:{port}");
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/health", () => Results.Ok(new { ok = true, service = "SmartPOS.LicenseWeb" }));

app.MapPost("/api/generate", (GenerateRequest req, HttpRequest http, IConfiguration config) =>
{
    var expectedUser = config["LICENSE_ADMIN_USER"] ?? "adminpos";
    var expectedPass = config["LICENSE_ADMIN_PASS"] ?? "adminpos123";

    var user = http.Headers["X-Admin-User"].ToString();
    var pass = http.Headers["X-Admin-Pass"].ToString();

    if (!string.Equals(user, expectedUser, StringComparison.Ordinal) ||
        !string.Equals(pass, expectedPass, StringComparison.Ordinal))
    {
        return Results.Unauthorized();
    }

    if (string.IsNullOrWhiteSpace(req.DeviceId) || string.IsNullOrWhiteSpace(req.Customer))
    {
        return Results.BadRequest(new { error = "DeviceId and Customer are required." });
    }

    if (req.Months is null && req.Days is null)
    {
        req = req with { Days = 14 };
    }

    var privPem = LoadPrivateKeyPem(config);
    if (string.IsNullOrWhiteSpace(privPem))
    {
        return Results.Problem("Missing private key. Configure LICENSE_PRIVATE_KEY_PEM or LICENSE_PRIVATE_KEY_PATH.");
    }

    var now = DateTimeOffset.UtcNow;
    DateTimeOffset expiresAt;
    int planMonths;

    if (req.Days.HasValue)
    {
        if (req.Days.Value <= 0)
        {
            return Results.BadRequest(new { error = "Days must be > 0." });
        }

        planMonths = 0;
        expiresAt = now.AddDays(req.Days.Value);
    }
    else
    {
        var months = req.Months!.Value;
        if (months is not (1 or 6 or 12))
        {
            return Results.BadRequest(new { error = "Months must be one of: 1, 6, 12." });
        }

        planMonths = months;
        expiresAt = now.AddMonths(months);
    }

    var payload = new LicensePayload(
        Version: 1,
        Customer: req.Customer.Trim(),
        DeviceId: req.DeviceId.Trim(),
        PlanMonths: planMonths,
        IssuedAtUtc: now,
        ExpiresAtUtc: expiresAt);

    var payloadJson = JsonSerializer.Serialize(payload);
    var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);

    using var ecdsa = ECDsa.Create();
    ecdsa.ImportFromPem(privPem);

    var sig = ecdsa.SignData(payloadBytes, HashAlgorithmName.SHA256);
    var token = $"SP1.{Base64Url.Encode(payloadBytes)}.{Base64Url.Encode(sig)}";

    return Results.Ok(new
    {
        token,
        expiresAtUtc = expiresAt,
        expiresAtLocal = expiresAt.ToLocalTime(),
        isTrial = planMonths == 0
    });
});

app.Run();

static string? LoadPrivateKeyPem(IConfiguration config)
{
    var pem = config["LICENSE_PRIVATE_KEY_PEM"];
    if (!string.IsNullOrWhiteSpace(pem))
    {
        if (pem.Contains("\\n", StringComparison.Ordinal))
        {
            pem = pem.Replace("\\n", "\n", StringComparison.Ordinal);
        }

        return pem;
    }

    var path = config["LICENSE_PRIVATE_KEY_PATH"];
    if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
    {
        return File.ReadAllText(path, Encoding.UTF8);
    }

    return null;
}

internal sealed record GenerateRequest(
    string DeviceId,
    string Customer,
    int? Months,
    int? Days
);

internal static class Base64Url
{
    public static string Encode(byte[] data)
    {
        return Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
