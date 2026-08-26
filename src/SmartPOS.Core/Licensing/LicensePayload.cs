using System.Text.Json.Serialization;
using System.Globalization;

namespace SmartPOS.Core.Licensing;

public sealed record LicensePayload(
    [property: JsonPropertyName("m")] string MachineId,
    [property: JsonPropertyName("p")] string PlanName,
    [property: JsonPropertyName("e")] string Expiry,
    [property: JsonPropertyName("g")] string? GeneratedAt,
    [property: JsonPropertyName("o")] int? OrderId,
    [property: JsonPropertyName("i")] int? OrderItemId,
    [property: JsonPropertyName("s")] int? Seat,
    [property: JsonPropertyName("pr")] string? ProductId
)
{
    public DateTimeOffset? ExpiresAtUtc
    {
        get
        {
            if (string.Equals(Expiry, "LIFETIME", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (!DateOnly.TryParseExact(Expiry, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            {
                return null;
            }

            return new DateTimeOffset(parsedDate.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
        }
    }
}
