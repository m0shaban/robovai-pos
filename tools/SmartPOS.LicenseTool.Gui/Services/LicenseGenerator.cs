using SmartPOS.Core.Licensing;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SmartPOS.LicenseTool.Gui.Services;

internal static class LicenseGenerator
{
    public static (bool Success, string Token, DateTimeOffset ExpiresAtUtc, string Error) Generate(
        string privateKeyPemPath,
        string deviceId,
        string customerName,
        LicenseDuration duration)
    {
        if (string.IsNullOrWhiteSpace(privateKeyPemPath) || !File.Exists(privateKeyPemPath))
        {
            return (false, string.Empty, default, "Private key path is invalid.");
        }

        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return (false, string.Empty, default, "Device ID is required.");
        }

        if (string.IsNullOrWhiteSpace(customerName))
        {
            return (false, string.Empty, default, "Customer name is required.");
        }

        var issued = DateTimeOffset.UtcNow;
        DateTimeOffset expires;
        int planMonths;

        if (duration.Kind == LicenseDurationKind.CustomDays)
        {
            if (duration.Days <= 0)
            {
                return (false, string.Empty, default, "Days must be > 0.");
            }

            planMonths = 0;
            expires = issued.AddDays(duration.Days);
        }
        else if (duration.Kind == LicenseDurationKind.Trial14Days)
        {
            planMonths = 0;
            expires = issued.AddDays(14);
        }
        else
        {
            planMonths = duration.Kind switch
            {
                LicenseDurationKind.Monthly1 => 1,
                LicenseDurationKind.SixMonths6 => 6,
                LicenseDurationKind.Yearly12 => 12,
                _ => 0
            };

            if (planMonths is not (1 or 6 or 12))
            {
                return (false, string.Empty, default, "Invalid duration.");
            }

            expires = issued.AddMonths(planMonths);
        }

        var planName = duration.Kind switch
        {
            LicenseDurationKind.Trial14Days => "TRIAL-14D",
            LicenseDurationKind.Monthly1 => "MONTHLY-1M",
            LicenseDurationKind.SixMonths6 => "SEMIANNUAL-6M",
            LicenseDurationKind.Yearly12 => "YEARLY-12M",
            LicenseDurationKind.CustomDays => $"CUSTOM-{duration.Days}D",
            _ => "CUSTOM"
        };

        var payload = new LicensePayload(
            MachineId: deviceId.Trim(),
            PlanName: planName,
            Expiry: expires.UtcDateTime.ToString("yyyy-MM-dd"),
            GeneratedAt: issued.UtcDateTime.ToString("O"),
            OrderId: null,
            OrderItemId: null,
            Seat: null,
            ProductId: customerName.Trim());

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = false });
        var payloadBytes = Encoding.UTF8.GetBytes(json);

        using var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(File.ReadAllText(privateKeyPemPath, Encoding.UTF8));

        var sig = ecdsa.SignData(payloadBytes, HashAlgorithmName.SHA256);
        var token = $"SP1.{Base64Url.Encode(payloadBytes)}.{Base64Url.Encode(sig)}";

        return (true, token, expires, string.Empty);
    }

    public static (bool Success, string PrivateKeyPath, string PublicKeyPath, string Error) GenerateKeys(string outputDirectory)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            return (false, string.Empty, string.Empty, "Output directory is required.");
        }

        Directory.CreateDirectory(outputDirectory);

        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var privPem = ecdsa.ExportECPrivateKeyPem();
        var pubPem = ecdsa.ExportSubjectPublicKeyInfoPem();

        var privPath = Path.Combine(outputDirectory, "smartpos-license-private.pem");
        var pubPath = Path.Combine(outputDirectory, "smartpos-license-public.pem");

        File.WriteAllText(privPath, privPem, Encoding.UTF8);
        File.WriteAllText(pubPath, pubPem, Encoding.UTF8);

        return (true, privPath, pubPath, string.Empty);
    }

    public static string? GetDefaultPrivateKeyPath()
    {
        try
        {
            var candidate = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "keys", "smartpos-license-private.pem"));
            return File.Exists(candidate) ? candidate : null;
        }
        catch
        {
            return null;
        }
    }

    public static string GetThisMachineDeviceId()
    {
        const string salt = "SmartPOS.DeviceId.v1";

        var raw = TryGetMachineGuid() ?? Environment.MachineName;
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw + "|" + salt));
        return ToBase32(bytes).Substring(0, 16);
    }

    private static string? TryGetMachineGuid()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Cryptography");
            return key?.GetValue("MachineGuid") as string;
        }
        catch
        {
            return null;
        }
    }

    private static string ToBase32(byte[] data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var output = new StringBuilder((data.Length * 8 + 4) / 5);

        int buffer = data[0];
        int next = 1;
        int bitsLeft = 8;
        while (bitsLeft > 0 || next < data.Length)
        {
            if (bitsLeft < 5)
            {
                if (next < data.Length)
                {
                    buffer <<= 8;
                    buffer |= data[next++] & 0xff;
                    bitsLeft += 8;
                }
                else
                {
                    var pad = 5 - bitsLeft;
                    buffer <<= pad;
                    bitsLeft += pad;
                }
            }

            var index = (buffer >> (bitsLeft - 5)) & 0x1f;
            bitsLeft -= 5;
            output.Append(alphabet[index]);
        }

        return output.ToString();
    }

    private static class Base64Url
    {
        public static string Encode(byte[] data)
        {
            return Convert.ToBase64String(data)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}

internal enum LicenseDurationKind
{
    Trial14Days,
    Monthly1,
    SixMonths6,
    Yearly12,
    CustomDays
}

internal readonly record struct LicenseDuration(LicenseDurationKind Kind, int Days = 0);
