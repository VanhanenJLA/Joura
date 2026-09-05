using System.Globalization;
using System.Reflection;

namespace Joura.Web.Services;

public sealed record BuildInfo(string? ShortSha, DateTimeOffset? TimestampUtc)
{
    public bool IsAvailable =>
        !string.IsNullOrWhiteSpace(ShortSha) &&
        TimestampUtc is not null;

    public string DisplayTimestamp => TimestampUtc?.UtcDateTime.ToString(
        "yyyy-MM-dd HH:mm 'UTC'",
        CultureInfo.InvariantCulture) ?? string.Empty;

    public string Tooltip => IsAvailable
        ? $"Build {ShortSha} from {TimestampUtc:O}"
        : string.Empty;

    public static BuildInfo FromAssembly(Assembly assembly)
    {
        var metadata = assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .ToDictionary(attribute => attribute.Key, attribute => attribute.Value);

        metadata.TryGetValue("BuildShortSha", out var shortSha);
        metadata.TryGetValue("BuildTimestampUtc", out var timestampValue);

        var timestampUtc = DateTimeOffset.TryParse(
            timestampValue,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var parsedTimestamp)
            ? parsedTimestamp
            : (DateTimeOffset?)null;

        return new BuildInfo(shortSha, timestampUtc);
    }
}
