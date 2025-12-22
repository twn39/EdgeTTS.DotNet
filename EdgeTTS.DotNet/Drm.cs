namespace EdgeTTS.DotNet;

using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Globalization; // 添加此 using 语句

/// <summary>
/// Handles DRM operations with clock skew correction.
/// </summary>
internal static class Drm
{
    private const long WinEpoch = 11644473600;
    private static double _clockSkewSeconds = 0.0;

    /// <summary>
    /// Adjusts the clock skew based on the server's Date header.
    /// </summary>
    public static void HandleClientResponseError(HttpResponseHeaders headers)
    {
        if (headers.Date.HasValue)
        {
            var serverDate = headers.Date.Value.ToUnixTimeSeconds();
            var clientDate = GetUnixTimestamp();
            _clockSkewSeconds += serverDate - clientDate;
        }
        else if (headers.TryGetValues("Date", out var dateValues)) // 尝试直接从字符串获取
        {
            var dateString = dateValues.FirstOrDefault();
            if (!string.IsNullOrEmpty(dateString))
            {
                var serverDate = ParseRfc2616Date(dateString);
                if (serverDate.HasValue)
                {
                    var clientDate = GetUnixTimestamp();
                    _clockSkewSeconds += serverDate.Value - clientDate;
                }
            }
        }
    }

    private static double GetUnixTimestamp()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds() + _clockSkewSeconds;
    }

    /// <summary>
    /// Parses an RFC 2616 date string into a Unix timestamp.
    /// Corresponds to Python's parse_rfc2616_date.
    /// </summary>
    private static long? ParseRfc2616Date(string date)
    {
        // Example: "Wed, 21 Oct 2015 07:28:00 GMT"
        // Python's %Z is tricky, C# DateTimeOffset.ParseExact is more robust.
        // We need to handle multiple possible formats.
        string[] formats = new[]
        {
            "ddd, dd MMM yyyy HH:mm:ss 'GMT'",
            "ddd, dd MMM yyyy HH:mm:ss Z", // For cases where Z is used for UTC
            "ddd, dd MMM yyyy HH:mm:ss K"  // For general UTC/offset
        };

        if (DateTimeOffset.TryParseExact(date, formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsedDate))
        {
            return parsedDate.ToUnixTimeSeconds();
        }
        return null;
    }

    /// <summary>
    /// Generates the Sec-MS-GEC token value.
    /// </summary>
    public static string GenerateSecMsGec()
    {
        var ticks = GetUnixTimestamp();

        // Switch to Windows file time epoch (1601-01-01 00:00:00 UTC)
        ticks += WinEpoch;

        // Round down to the nearest 5 minutes (300 seconds)
        ticks -= ticks % 300;

        // Convert the ticks to 100-nanosecond intervals (Windows file time format)
        ticks *= 10_000_000; // 1 second = 10,000,000 100-nanosecond intervals

        // Create the string to hash by concatenating the ticks and the trusted client token
        var strToHash = $"{ticks:F0}{Constants.TrustedClientToken}"; // Use F0 to ensure no decimal places

        // Compute the SHA256 hash and return the uppercased hex digest
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(strToHash));

        return BitConverter.ToString(hash).Replace("-", "").ToUpper();
    }

    /// <summary>
    /// Generates a random MUID.
    /// </summary>
    public static string GenerateMuid()
    {
        return Guid.NewGuid().ToString("N").ToUpper();
    }
}