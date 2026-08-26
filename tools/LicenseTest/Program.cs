using Microsoft.Extensions.Configuration;
using SmartPOS.Infrastructure.Services;

// Build configuration exactly like App.xaml.cs does (Host.CreateDefaultBuilder reads appsettings.json)
var config = new ConfigurationBuilder()
    .SetBasePath(Path.Combine(AppContext.BaseDirectory))
    .AddJsonFile("appsettings.json", optional: true)
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["LicenseActivation:OfflineSecretKey"] = "mohamedshabanibrahimsalamaetmanrobovai",
        ["LicenseActivation:SecretSalt"] = "store.license-key.v1",
        ["LicenseActivation:EnableOnlineVerification"] = "false"
    })
    .Build();

var licenseService = new LicenseService(config);

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("=== SmartPOS License Integration Test ===");
Console.WriteLine($"Device ID: {licenseService.GetDeviceId()}");
Console.WriteLine();

// Test 1: Generate key with C# KeyGen logic
var deviceId = licenseService.GetDeviceId().Trim().ToUpperInvariant();
Console.WriteLine($"Normalized Device ID: [{deviceId}]");

// Generate key using exact same algo
var secret = "mohamedshabanibrahimsalamaetmanrobovai";
var salt = "store.license-key.v1";
var payloadJson = System.Text.Json.JsonSerializer.Serialize(new
{
    m = deviceId,
    p = "Pro",
    g = DateTime.UtcNow.ToString("yyyy-MM-dd"),
    e = "LIFETIME"
}, new System.Text.Json.JsonSerializerOptions { WriteIndented = false });

Console.WriteLine($"Payload JSON: {payloadJson}");

var payloadB64 = Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(payloadJson));
Console.WriteLine($"Payload B64: {payloadB64}");

var secretBytes = System.Text.Encoding.UTF8.GetBytes($"{secret}:{salt}");
var hmacDigest = System.Security.Cryptography.HMACSHA256.HashData(secretBytes, System.Text.Encoding.UTF8.GetBytes(payloadB64));
var signature = Base64UrlEncode(hmacDigest);
Console.WriteLine($"Signature: {signature}");

var token = $"{payloadB64}.{signature}";
Console.WriteLine($"\nGenerated Token:\n{token}");
Console.WriteLine();

// Test 2: Activate with the generated key
Console.WriteLine("--- Testing ActivateAsync ---");
var status = await licenseService.ActivateAsync(token);
Console.WriteLine($"IsValid: {status.IsValid}");
Console.WriteLine($"IsInGrace: {status.IsInGrace}");
Console.WriteLine($"DaysRemaining: {status.DaysRemaining}");
Console.WriteLine($"Reason: {status.Reason}");
Console.WriteLine($"ExpiresAtUtc: {status.ExpiresAtUtc}");
Console.WriteLine();

// Test 3: GetStatus
Console.WriteLine("--- Testing GetStatusAsync ---");
var status2 = await licenseService.GetStatusAsync();
Console.WriteLine($"IsValid: {status2.IsValid}");
Console.WriteLine($"Reason: {status2.Reason}");
Console.WriteLine();

if (status.IsValid)
{
    Console.WriteLine(">>> ACTIVATION TEST PASSED <<<");
}
else
{
    Console.WriteLine(">>> ACTIVATION TEST FAILED <<<");
    Console.WriteLine("Copying token to clipboard for manual testing...");
    try
    {
        var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("clip") { RedirectStandardInput = true });
        p?.StandardInput.Write(token);
        p?.StandardInput.Close();
        p?.WaitForExit();
    }
    catch { }
}

static string Base64UrlEncode(byte[] data)
    => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
