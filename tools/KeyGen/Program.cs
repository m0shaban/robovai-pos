using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

// ─── CONFIG ──────────────────────────────────────────
const string OfflineSecret = "mohamedshabanibrahimsalamaetmanrobovai";
const string SecretSalt    = "store.license-key.v1";
// ─────────────────────────────────────────────────────

string deviceId = args.Length > 0 ? args[0].Trim().ToUpperInvariant() : "VQYAFH5I4W347PFQ";
string expiry   = args.Length > 1 ? args[1] : "LIFETIME";
string plan     = args.Length > 2 ? args[2] : "Pro";

// Build payload JSON (exact same field order as Python generator)
var payloadObj = new Dictionary<string, object?>
{
    ["m"] = deviceId,
    ["p"] = plan,
    ["g"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
    ["e"] = expiry
};

var payloadJson = JsonSerializer.Serialize(payloadObj, new JsonSerializerOptions
{
    WriteIndented = false
});

Console.WriteLine($"Device ID : {deviceId}");
Console.WriteLine($"Plan      : {plan}");
Console.WriteLine($"Expiry    : {expiry}");
Console.WriteLine($"Payload   : {payloadJson}");
Console.WriteLine();

// Base64Url encode payload
var payloadB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
Console.WriteLine($"Payload B64: {payloadB64}");

// Compute signature (EXACT same algorithm as LicenseService.ComputeSignature)
var secretBytes = Encoding.UTF8.GetBytes($"{OfflineSecret}:{SecretSalt}");
var payloadB64Bytes = Encoding.UTF8.GetBytes(payloadB64);
var digest = HMACSHA256.HashData(secretBytes, payloadB64Bytes);
var signature = Base64UrlEncode(digest);
Console.WriteLine($"Signature  : {signature}");
Console.WriteLine();

// Full token
var token = $"{payloadB64}.{signature}";
Console.WriteLine("═══════════════════════════════════════════════");
Console.WriteLine("LICENSE KEY:");
Console.WriteLine(token);
Console.WriteLine("═══════════════════════════════════════════════");

// Self-verify
var parts = token.Split('.');
var verifyDigest = HMACSHA256.HashData(secretBytes, Encoding.UTF8.GetBytes(parts[0]));
var verifySig = Base64UrlEncode(verifyDigest);
Console.WriteLine($"\nSelf-check : sig match = {verifySig == parts[1]}");

// Copy to clipboard on Windows
try
{
    var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("clip") { RedirectStandardInput = true });
    p?.StandardInput.Write(token);
    p?.StandardInput.Close();
    p?.WaitForExit();
    Console.WriteLine("Copied to clipboard!");
}
catch { }

static string Base64UrlEncode(byte[] data)
    => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
