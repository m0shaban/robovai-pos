using SmartPOS.Core.Licensing;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

const string AdminEmail = "adminpos";
const string AdminPassword = "adminpos123";

static int Usage()
{
    Console.WriteLine("SmartPOS.LicenseTool");
    Console.WriteLine("Commands:");
    Console.WriteLine("  gen-keys --out <dir> [--email <email> --password <password>]");
    Console.WriteLine("  gen-code --private-key <pemPath> --device <deviceId> --customer <name> [--months <1|6|12> | --days <N>] [--email <email> --password <password>]");
    Console.WriteLine("  gen-trial --private-key <pemPath> --device <deviceId> --customer <name> [--days 14] [--email <email> --password <password>]");
    Console.WriteLine("  show-device");
    Console.WriteLine("\nNo args: runs an interactive wizard.");
    return 2;
}

if (args.Length == 0)
{
    return Wizard();
}

var command = args[0].Trim().ToLowerInvariant();
var options = ParseOptions(args.Skip(1).ToArray());

if (command is not "show-device")
{
    var ok = EnsureAuthenticated(options);
    if (!ok)
    {
        Console.WriteLine("Authentication failed.");
        return 3;
    }
}

return command switch
{
    "gen-keys" => GenKeys(options),
    "gen-code" => GenCode(options),
    "gen-trial" => GenTrial(options),
    "show-device" => ShowDevice(),
    _ => Usage()
};

static Dictionary<string, string> ParseOptions(string[] args)
{
    var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (int i = 0; i < args.Length; i++)
    {
        var a = args[i];
        if (!a.StartsWith("--", StringComparison.Ordinal))
        {
            continue;
        }

        var key = a[2..];
        var value = (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
            ? args[++i]
            : "true";

        dict[key] = value;
    }

    return dict;
}

static int GenKeys(Dictionary<string, string> opt)
{
    if (!opt.TryGetValue("out", out var outDir) || string.IsNullOrWhiteSpace(outDir))
    {
        Console.WriteLine("Missing --out");
        return 2;
    }

    Directory.CreateDirectory(outDir);

    using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    var privPem = ecdsa.ExportECPrivateKeyPem();
    var pubPem = ecdsa.ExportSubjectPublicKeyInfoPem();

    var privPath = Path.Combine(outDir, "smartpos-license-private.pem");
    var pubPath = Path.Combine(outDir, "smartpos-license-public.pem");

    File.WriteAllText(privPath, privPem, Encoding.UTF8);
    File.WriteAllText(pubPath, pubPem, Encoding.UTF8);

    Console.WriteLine($"Wrote: {privPath}");
    Console.WriteLine($"Wrote: {pubPath}");
    return 0;
}

static int GenCode(Dictionary<string, string> opt)
{
    if (!opt.TryGetValue("private-key", out var privPath) || string.IsNullOrWhiteSpace(privPath) || !File.Exists(privPath))
    {
        Console.WriteLine("Missing/invalid --private-key");
        return 2;
    }

    if (!opt.TryGetValue("device", out var device) || string.IsNullOrWhiteSpace(device))
    {
        Console.WriteLine("Missing --device");
        return 2;
    }

    if (!opt.TryGetValue("customer", out var customer) || string.IsNullOrWhiteSpace(customer))
    {
        Console.WriteLine("Missing --customer");
        return 2;
    }

    var result = TryGenerateToken(privPath, device, customer, opt);
    if (!result.Success)
    {
        Console.WriteLine(result.Error);
        return 2;
    }

    Console.WriteLine(result.Token);
    return 0;
}

static int GenTrial(Dictionary<string, string> opt)
{
    if (!opt.ContainsKey("days"))
    {
        opt["days"] = "14";
    }

    return GenCode(opt);
}

static int ShowDevice()
{
    Console.WriteLine(GetDeviceId());
    return 0;
}

static int Wizard()
{
    Console.WriteLine("SmartPOS.LicenseTool - Wizard");
    Console.WriteLine("--------------------------------");

    if (!EnsureAuthenticated(null))
    {
        Console.WriteLine("Authentication failed.");
        return 3;
    }

    var defaultPriv = GetDefaultPrivateKeyPath();
    if (!string.IsNullOrWhiteSpace(defaultPriv))
    {
        Console.WriteLine($"Tip: Press Enter to use default private key: {defaultPriv}");
    }

    Console.Write("Private key PEM path: ");
    var privPathInput = (Console.ReadLine() ?? string.Empty).Trim().Trim('"');
    var privPath = string.IsNullOrWhiteSpace(privPathInput) ? (defaultPriv ?? string.Empty) : privPathInput;
    if (string.IsNullOrWhiteSpace(privPath) || !File.Exists(privPath))
    {
        Console.WriteLine("Invalid private key path.");
        return 2;
    }

    Console.Write("Customer name: ");
    var customer = (Console.ReadLine() ?? string.Empty).Trim();
    if (string.IsNullOrWhiteSpace(customer))
    {
        Console.WriteLine("Customer is required.");
        return 2;
    }

    Console.Write("Device ID: ");
    var device = (Console.ReadLine() ?? string.Empty).Trim();
    if (string.IsNullOrWhiteSpace(device))
    {
        Console.WriteLine("Device ID is required.");
        return 2;
    }

    Console.WriteLine("\nChoose duration:");
    Console.WriteLine("  1) Trial (14 days)");
    Console.WriteLine("  2) Monthly (1 month)");
    Console.WriteLine("  3) 6 months");
    Console.WriteLine("  4) Yearly (12 months)");
    Console.WriteLine("  5) Custom days");
    Console.Write("Select [1-5]: ");
    var choice = (Console.ReadLine() ?? string.Empty).Trim();

    var opt = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["private-key"] = privPath,
        ["customer"] = customer,
        ["device"] = device
    };

    switch (choice)
    {
        case "1":
            opt["days"] = "14";
            break;
        case "2":
            opt["months"] = "1";
            break;
        case "3":
            opt["months"] = "6";
            break;
        case "4":
            opt["months"] = "12";
            break;
        case "5":
            Console.Write("Days: ");
            var daysText = (Console.ReadLine() ?? string.Empty).Trim();
            opt["days"] = daysText;
            break;
        default:
            Console.WriteLine("Invalid selection.");
            return 2;
    }

    var result = TryGenerateToken(privPath, device, customer, opt);
    if (!result.Success)
    {
        Console.WriteLine(result.Error);
        return 2;
    }

    Console.WriteLine("\nActivation code:");
    Console.WriteLine(result.Token);
    Console.WriteLine($"Expires (UTC): {result.ExpiresAtUtc:yyyy-MM-dd HH:mm}  |  Expires (Local): {result.ExpiresAtUtc.ToLocalTime():yyyy-MM-dd HH:mm}");
    return 0;
}

static bool EnsureAuthenticated(Dictionary<string, string>? opt)
{
    if (opt is not null
        && opt.TryGetValue("email", out var emailArg)
        && opt.TryGetValue("password", out var passwordArg)
        && IsValidCredentials(emailArg, passwordArg))
    {
        return true;
    }

    for (var attempt = 1; attempt <= 3; attempt++)
    {
        Console.Write("Email: ");
        var email = (Console.ReadLine() ?? string.Empty).Trim();

        Console.Write("Password: ");
        var password = ReadPassword();

        if (IsValidCredentials(email, password))
        {
            return true;
        }

        Console.WriteLine("Invalid credentials.");
    }

    return false;
}

static bool IsValidCredentials(string email, string password)
{
    return string.Equals(email?.Trim(), AdminEmail, StringComparison.Ordinal)
        && string.Equals(password ?? string.Empty, AdminPassword, StringComparison.Ordinal);
}

static string ReadPassword()
{
    var sb = new StringBuilder();

    while (true)
    {
        var key = Console.ReadKey(intercept: true);

        if (key.Key == ConsoleKey.Enter)
        {
            Console.WriteLine();
            break;
        }

        if (key.Key == ConsoleKey.Backspace)
        {
            if (sb.Length > 0)
            {
                sb.Length--;
                Console.Write("\b \b");
            }
            continue;
        }

        if (!char.IsControl(key.KeyChar))
        {
            sb.Append(key.KeyChar);
            Console.Write('*');
        }
    }

    return sb.ToString();
}

static string? GetDefaultPrivateKeyPath()
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

static (bool Success, string Token, DateTimeOffset ExpiresAtUtc, string Error) TryGenerateToken(
    string privPath,
    string device,
    string customer,
    Dictionary<string, string> opt)
{
    int planMonths;
    DateTimeOffset issued = DateTimeOffset.UtcNow;
    DateTimeOffset expires;

    if (opt.TryGetValue("days", out var daysText) && int.TryParse(daysText, out var days) && days > 0)
    {
        planMonths = 0;
        expires = issued.AddDays(days);
    }
    else
    {
        if (!opt.TryGetValue("months", out var monthsText) || !int.TryParse(monthsText, out planMonths) || (planMonths != 1 && planMonths != 6 && planMonths != 12))
        {
            return (false, string.Empty, default, "Invalid duration. Use --months (1, 6, 12) or --days (N)");
        }

        expires = issued.AddMonths(planMonths);
    }

    var payload = new LicensePayload(
        Version: 1,
        Customer: customer,
        DeviceId: device.Trim(),
        PlanMonths: planMonths,
        IssuedAtUtc: issued,
        ExpiresAtUtc: expires);

    var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = false });
    var payloadBytes = Encoding.UTF8.GetBytes(json);

    using var ecdsa = ECDsa.Create();
    ecdsa.ImportFromPem(File.ReadAllText(privPath, Encoding.UTF8));

    var sig = ecdsa.SignData(payloadBytes, HashAlgorithmName.SHA256);
    var token = $"SP1.{Base64Url.Encode(payloadBytes)}.{Base64Url.Encode(sig)}";
    return (true, token, expires, string.Empty);
}

static string GetDeviceId()
{
    const string salt = "SmartPOS.DeviceId.v1";

    var raw = TryGetMachineGuid() ?? Environment.MachineName;
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw + "|" + salt));
    return ToBase32(bytes).Substring(0, 16);
}

static string? TryGetMachineGuid()
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

static string ToBase32(byte[] data)
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

static class Base64Url
{
    public static string Encode(byte[] data)
    {
        return Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
