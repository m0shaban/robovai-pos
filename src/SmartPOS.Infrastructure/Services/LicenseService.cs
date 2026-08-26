using SmartPOS.Core.Interfaces;
using SmartPOS.Core.Licensing;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SmartPOS.Infrastructure.Services;

public sealed class LicenseService : ILicenseService
{
    private const int TrialDays = 14;
    private const int GraceDays = 3;
    private const string DefaultVerifyEndpoint = "https://robovai.tech/api/licenses/verify/";
    private const string DefaultSalt = "store.license-key.v1";
    private static readonly Regex MachineIdRegex = new("^[A-Z0-9_-]{8,64}$", RegexOptions.Compiled);
    private static readonly HttpClient HttpClient = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _offlineSecret;
    private readonly string _secretSalt;
    private readonly string _verifyEndpoint;
    private readonly bool _enableOnlineVerification;
    private readonly int _onlineTimeoutSeconds;

    public LicenseService(IConfiguration configuration)
    {
        var section = configuration.GetSection("LicenseActivation");
        _offlineSecret = section["OfflineSecretKey"]?.Trim() ?? string.Empty;
        _secretSalt = section["SecretSalt"]?.Trim() ?? DefaultSalt;
        _verifyEndpoint = section["VerifyEndpoint"]?.Trim() ?? DefaultVerifyEndpoint;
        _enableOnlineVerification = bool.TryParse(section["EnableOnlineVerification"], out var onlineEnabled)
            ? onlineEnabled
            : true;
        _onlineTimeoutSeconds = int.TryParse(section["OnlineTimeoutSeconds"], out var timeout)
            ? Math.Max(5, timeout)
            : 20;
    }

    public string GetDeviceId() => DeviceIdProvider.GetDeviceId();

    public async Task<LicenseStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var deviceId = NormalizeMachineId(GetDeviceId());
        var state = await LoadStateAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(state.ActivationCode))
        {
            var validation = ValidateTokenOffline(state.ActivationCode, deviceId);
            if (validation.IsValidOrGrace)
            {
                var now = DateTimeOffset.UtcNow;
                var last = state.LastValidatedUtc;

                if (last.HasValue && now < last.Value.AddHours(-6))
                    return new LicenseStatus(false, false, 0, validation.Payload?.ExpiresAtUtc, "System time changed");

                var newState = state with
                {
                    LastValidatedUtc = Max(last, now),
                    CachedExpiresAtUtc = validation.Payload?.ExpiresAtUtc,
                    LastVerifiedOnline = false,
                    LastReason = validation.Status.Reason
                };
                await SaveStateAsync(newState, cancellationToken);
                return validation.Status;
            }

            if (state.LastVerifiedOnline)
            {
                var cachedStatus = BuildStatusFromExpiry(state.CachedExpiresAtUtc,
                    state.LastReason ?? "Active (cached online validation)");
                if (cachedStatus.IsValid || cachedStatus.IsInGrace)
                    return cachedStatus;
            }

            return validation.Status;
        }

        if (state.TrialStartedAtUtc.HasValue)
        {
            var now = DateTimeOffset.UtcNow;
            var last = state.LastValidatedUtc;
            var trialExpiry = state.TrialStartedAtUtc.Value.AddDays(TrialDays);

            if (last.HasValue && now < last.Value.AddHours(-6))
                return new LicenseStatus(false, false, 0, trialExpiry, "System time changed", true);

            var newState = state with { LastValidatedUtc = Max(last, now) };
            await SaveStateAsync(newState, cancellationToken);

            if (now <= trialExpiry)
            {
                var daysLeft = (int)Math.Ceiling((trialExpiry - now).TotalDays);
                return new LicenseStatus(true, false, Math.Max(0, daysLeft), trialExpiry, "Trial active", true);
            }

            return new LicenseStatus(false, false, 0, trialExpiry, "Trial expired", true);
        }

        return new LicenseStatus(false, false, 0, null, "Not activated");
    }

    public async Task<LicenseStatus> ActivateAsync(string activationCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(activationCode))
            return new LicenseStatus(false, false, 0, null, "Empty code");

        activationCode = activationCode.Trim();
        var deviceId = NormalizeMachineId(GetDeviceId());

        // 1) Try offline token validation
        var validation = ValidateTokenOffline(activationCode, deviceId);
        if (validation.IsValidOrGrace)
        {
            var now = DateTimeOffset.UtcNow;
            var state = new LicenseState(
                activationCode, now,
                TrialStartedAtUtc: null,
                CachedExpiresAtUtc: validation.Payload?.ExpiresAtUtc,
                LastVerifiedOnline: false,
                LastReason: validation.Status.Reason);
            await SaveStateAsync(state, cancellationToken);
            return validation.Status;
        }

        // 2) Try online verification
        if (_enableOnlineVerification && !string.IsNullOrWhiteSpace(_verifyEndpoint))
        {
            try
            {
                var online = await VerifyOnlineAsync(activationCode, deviceId, cancellationToken);
                if (online.IsValid)
                {
                    var now = DateTimeOffset.UtcNow;
                    var state = new LicenseState(
                        activationCode, now,
                        TrialStartedAtUtc: null,
                        CachedExpiresAtUtc: online.ExpiresAtUtc,
                        LastVerifiedOnline: true,
                        LastReason: online.Reason);
                    await SaveStateAsync(state, cancellationToken);
                    return BuildStatusFromExpiry(online.ExpiresAtUtc, online.Reason);
                }
            }
            catch
            {
                // Network unavailable — fall through
            }
        }

        // Return the offline validation failure reason (more descriptive)
        return validation.Status;
    }

    public async Task<LicenseStatus> StartTrialAsync(CancellationToken cancellationToken = default)
    {
        var state = await LoadStateAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(state.ActivationCode))
            return await GetStatusAsync(cancellationToken);

        if (!state.TrialStartedAtUtc.HasValue)
        {
            var now = DateTimeOffset.UtcNow;
            state = state with { TrialStartedAtUtc = now, LastValidatedUtc = now };
            await SaveStateAsync(state, cancellationToken);
        }

        return await GetStatusAsync(cancellationToken);
    }

    // ─── Private Helpers ─────────────────────────────────────────────────────────

    private static DateTimeOffset? Max(DateTimeOffset? a, DateTimeOffset b)
        => !a.HasValue ? b : (a.Value > b ? a : b);

    private LicenseValidationResult ValidateTokenOffline(string token, string deviceId)
    {
        // If no offline secret configured, skip offline check (not an error)
        if (string.IsNullOrWhiteSpace(_offlineSecret))
            return LicenseValidationResult.Invalid("Offline secret not configured — use online activation");

        try
        {
            var parts = token.Split('.', StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
                return LicenseValidationResult.Invalid("صيغة الكود غير صحيحة. تأكد من نسخ الكود كاملاً.");

            var payloadB64 = parts[0];
            var signatureB64 = parts[1];
            var expectedSignature = ComputeSignature(payloadB64);

            if (!ConstantTimeEquals(signatureB64, expectedSignature))
                return LicenseValidationResult.Invalid("كود التفعيل غير صحيح أو منتهي الصلاحية.");

            var payloadBytes = Base64Url.Decode(payloadB64);
            var payloadJson = Encoding.UTF8.GetString(payloadBytes);
            var payload = JsonSerializer.Deserialize<LicensePayload>(payloadJson, JsonOptions);

            if (payload is null)
                return LicenseValidationResult.Invalid("بيانات الكود تالفة.");

            var tokenDeviceId = NormalizeMachineId(payload.MachineId ?? string.Empty);
            if (!string.Equals(tokenDeviceId, deviceId, StringComparison.Ordinal))
                return LicenseValidationResult.Invalid($"كود التفعيل مخصص لجهاز آخر.\nجهازك: {deviceId}\nالكود لـ: {tokenDeviceId}");

            if (!string.Equals(payload.Expiry, "LIFETIME", StringComparison.OrdinalIgnoreCase)
                && payload.ExpiresAtUtc is null)
                return LicenseValidationResult.Invalid("تنسيق تاريخ انتهاء الكود غير صحيح.");

            return LicenseValidationResult.FromStatus(payload, BuildStatusFromExpiry(payload.ExpiresAtUtc, "Active"));
        }
        catch (Exception ex)
        {
            return LicenseValidationResult.Invalid($"خطأ في قراءة الكود: {ex.Message}");
        }
    }

    private async Task<OnlineValidationResult> VerifyOnlineAsync(string activationCode, string deviceId, CancellationToken cancellationToken)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(_onlineTimeoutSeconds));

            var response = await HttpClient.PostAsJsonAsync(
                _verifyEndpoint,
                new { license_key = activationCode, machine_id = deviceId },
                cts.Token);

            using var json = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cts.Token),
                cancellationToken: cts.Token);
            var root = json.RootElement;

            var valid = root.TryGetProperty("valid", out var validProp) && validProp.ValueKind == JsonValueKind.True;
            var reason = root.TryGetProperty("reason", out var reasonProp)
                ? reasonProp.GetString() ?? "online_validation"
                : (response.IsSuccessStatusCode ? "online_validation" : "invalid_license");

            DateTimeOffset? expiresAt = null;
            if (root.TryGetProperty("expires_at", out var expiresProp) && expiresProp.ValueKind == JsonValueKind.String)
            {
                if (DateTimeOffset.TryParse(expiresProp.GetString(), CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal, out var parsed))
                    expiresAt = parsed.ToUniversalTime();
            }

            return new OnlineValidationResult(valid, expiresAt, reason);
        }
        catch
        {
            return new OnlineValidationResult(false, null, "لا يوجد اتصال بالإنترنت");
        }
    }

    private string ComputeSignature(string payloadB64)
    {
        var secret = Encoding.UTF8.GetBytes($"{_offlineSecret}:{_secretSalt}");
        var payloadBytes = Encoding.UTF8.GetBytes(payloadB64);
        var digest = HMACSHA256.HashData(secret, payloadBytes);
        return Base64Url.Encode(digest);
    }

    private static bool ConstantTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static LicenseStatus BuildStatusFromExpiry(DateTimeOffset? expiresAtUtc, string reason)
    {
        if (expiresAtUtc is null)
            return new LicenseStatus(true, false, 0, null, reason);  // LIFETIME

        var now = DateTimeOffset.UtcNow;
        if (now <= expiresAtUtc.Value)
        {
            var daysLeft = (int)Math.Ceiling((expiresAtUtc.Value - now).TotalDays);
            return new LicenseStatus(true, false, Math.Max(0, daysLeft), expiresAtUtc, reason);
        }

        var graceUntil = expiresAtUtc.Value.AddDays(GraceDays);
        if (now <= graceUntil)
        {
            var daysLeft = (int)Math.Ceiling((graceUntil - now).TotalDays);
            return new LicenseStatus(false, true, Math.Max(0, daysLeft), expiresAtUtc, "Expired - grace period");
        }

        return new LicenseStatus(false, false, 0, expiresAtUtc, "Expired");
    }

    private static string NormalizeMachineId(string machineId)
        => (machineId ?? string.Empty).Trim().ToUpperInvariant().Replace(" ", string.Empty);

    // ─── State persistence ────────────────────────────────────────────────────────

    private static string GetStatePath()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartPOS");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "license.json");
    }

    private static async Task<LicenseState> LoadStateAsync(CancellationToken cancellationToken)
    {
        try
        {
            var path = GetStatePath();
            if (!File.Exists(path)) return new LicenseState(null, null);
            var encryptedBase64 = await File.ReadAllTextAsync(path, cancellationToken);
            var json = DecryptState(encryptedBase64);
            return JsonSerializer.Deserialize<LicenseState>(json, JsonOptions) ?? new LicenseState(null, null);
        }
        catch { return new LicenseState(null, null); }
    }

    private static async Task SaveStateAsync(LicenseState state, CancellationToken cancellationToken)
    {
        var path = GetStatePath();
        var json = JsonSerializer.Serialize(state, JsonOptions);
        var encryptedBase64 = EncryptState(json);
        await File.WriteAllTextAsync(path, encryptedBase64, Encoding.UTF8, cancellationToken);
    }

    private static string EncryptState(string plainText)
    {
        var key = SHA256.HashData(Encoding.UTF8.GetBytes(DeviceIdProvider.GetDeviceId() + "SecretSalt"));
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = key;
        aes.GenerateIV();
        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        ms.Write(aes.IV, 0, aes.IV.Length);
        using (var cs = new System.Security.Cryptography.CryptoStream(ms, encryptor, System.Security.Cryptography.CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }
        return Convert.ToBase64String(ms.ToArray());
    }

    private static string DecryptState(string cipherText)
    {
        var fullCipher = Convert.FromBase64String(cipherText);
        var key = SHA256.HashData(Encoding.UTF8.GetBytes(DeviceIdProvider.GetDeviceId() + "SecretSalt"));
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = key;
        var iv = new byte[aes.BlockSize / 8];
        Array.Copy(fullCipher, 0, iv, 0, iv.Length);
        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length);
        using var cs = new System.Security.Cryptography.CryptoStream(ms, decryptor, System.Security.Cryptography.CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        return sr.ReadToEnd();
    }

    // ─── Inner Types ─────────────────────────────────────────────────────────────

    private sealed record LicenseState(
        string? ActivationCode,
        DateTimeOffset? LastValidatedUtc,
        DateTimeOffset? TrialStartedAtUtc = null,
        DateTimeOffset? CachedExpiresAtUtc = null,
        bool LastVerifiedOnline = false,
        string? LastReason = null);

    private sealed record LicenseValidationResult(LicensePayload? Payload, LicenseStatus Status, bool IsValidOrGrace)
    {
        public static LicenseValidationResult FromStatus(LicensePayload payload, LicenseStatus status)
            => new(payload, status, status.IsValid || status.IsInGrace);
        public static LicenseValidationResult Invalid(string reason)
            => new(null, new LicenseStatus(false, false, 0, null, reason), false);
    }

    private sealed record OnlineValidationResult(bool IsValid, DateTimeOffset? ExpiresAtUtc, string Reason);

    private static class Base64Url
    {
        public static string Encode(byte[] data)
            => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        public static byte[] Decode(string text)
        {
            var s = text.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 2: s += "=="; break;
                case 3: s += "="; break;
            }
            return Convert.FromBase64String(s);
        }
    }

    private static class DeviceIdProvider
    {
        private const string Salt = "SmartPOS.DeviceId.v1";

        public static string GetDeviceId()
        {
            var raw = TryGetMachineGuid() ?? Environment.MachineName;
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw + "|" + Salt));
            return ToBase32(bytes).Substring(0, 16);
        }

        private static string? TryGetMachineGuid()
        {
            try
            {
                if (!OperatingSystem.IsWindows()) return null;
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Cryptography");
                return key?.GetValue("MachineGuid") as string;
            }
            catch { return null; }
        }

        private static string ToBase32(byte[] data)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            var output = new StringBuilder((data.Length * 8 + 4) / 5);
            int buffer = data[0], next = 1, bitsLeft = 8;
            while (bitsLeft > 0 || next < data.Length)
            {
                if (bitsLeft < 5)
                {
                    if (next < data.Length) { buffer <<= 8; buffer |= data[next++] & 0xff; bitsLeft += 8; }
                    else { var pad = 5 - bitsLeft; buffer <<= pad; bitsLeft += pad; }
                }
                var index = (buffer >> (bitsLeft - 5)) & 0x1f;
                bitsLeft -= 5;
                output.Append(alphabet[index]);
            }
            return output.ToString();
        }
    }
}
