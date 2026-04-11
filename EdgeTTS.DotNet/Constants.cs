namespace EdgeTTS.DotNet;

/// <summary>
/// Contains constants for the EdgeTTS library.
/// </summary>
internal static class Constants
{
    public const string BaseUrl = "speech.platform.bing.com/consumer/speech/synthesize/readaloud";
    public const string TrustedClientToken = "6A5AA1D4EAFF4E9FB37E23D68491D6F4";

    public static readonly string WssUrl = $"wss://{BaseUrl}/edge/v1?TrustedClientToken={TrustedClientToken}";
    public static readonly string VoiceListUrl = $"https://{BaseUrl}/voices/list?trustedclienttoken={TrustedClientToken}";

    public const string DefaultVoice = "en-US-EmmaMultilingualNeural";

    private const string ChromiumFullVersion = "143.0.3650.75";
    private static readonly string ChromiumMajorVersion = ChromiumFullVersion.Split('.')[0];
    public static readonly string SecMsGecVersion = $"1-{ChromiumFullVersion}";

    public static readonly Dictionary<string, string> BaseHeaders = new()
    {
        { "User-Agent", $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{ChromiumMajorVersion}.0.0.0 Safari/537.36 Edg/{ChromiumMajorVersion}.0.0.0" },
        { "Accept-Encoding", "gzip, deflate, br, zstd" },
        { "Accept-Language", "en-US,en;q=0.9" }
    };

    public static readonly Dictionary<string, string> WssHeaders = new()
    {
        { "Pragma", "no-cache" },
        { "Cache-Control", "no-cache" },
        { "Origin", "chrome-extension://jdiccldimpdaibmpdkjnbmckianbfold" },
        { "User-Agent", BaseHeaders["User-Agent"] },
        { "Accept-Language", BaseHeaders["Accept-Language"] }
    };

    public static readonly Dictionary<string, string> VoiceHeaders = new()
    {
        { "Authority", "speech.platform.bing.com" },
        { "Sec-CH-UA", $"\" Not;A Brand\";v=\"99\", \"Microsoft Edge\";v=\"{ChromiumMajorVersion}\", \"Chromium\";v=\"{ChromiumMajorVersion}\"" },
        { "Sec-CH-UA-Mobile", "?0" },
        { "Accept", "*/*" },
        { "Sec-Fetch-Site", "none" },
        { "Sec-Fetch-Mode", "cors" },
        { "Sec-Fetch-Dest", "empty" },
        { "User-Agent", BaseHeaders["User-Agent"] },
        { "Accept-Encoding", BaseHeaders["Accept-Encoding"] },
        { "Accept-Language", BaseHeaders["Accept-Language"] }
    };

    // Audio timing constants for CBR-based offset compensation.
    public const long TicksPerSecond = 10_000_000;
    public const int Mp3BitrateBps = 48_000;
}