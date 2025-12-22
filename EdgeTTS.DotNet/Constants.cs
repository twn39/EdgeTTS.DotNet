namespace EdgeTTS.DotNet;

/// <summary>
/// Contains constants for the EdgeTTS library.
/// </summary>
internal static class Constants
{
    public const string BaseUrl = "api.msedgeservices.com/tts/cognitiveservices";
    public const string TrustedClientToken = "6A5AA1D4EAFF4E9FB37E23D68491D6F4";

    public static readonly string WssUrl = $"wss://{BaseUrl}/websocket/v1?Ocp-Apim-Subscription-Key={TrustedClientToken}";
    public static readonly string VoiceListUrl = $"https://{BaseUrl}/voices/list?Ocp-Apim-Subscription-Key={TrustedClientToken}";

    public const string DefaultVoice = "en-US-EmmaMultilingualNeural";

    private const string ChromiumFullVersion = "126.0.6478.127";
    private static readonly string ChromiumMajorVersion = ChromiumFullVersion.Split('.')[0];
    public static readonly string SecMsGecVersion = $"1-{ChromiumFullVersion}";

    public static readonly Dictionary<string, string> WssHeaders = new()
    {
        { "Pragma", "no-cache" },
        { "Cache-Control", "no-cache" },
        { "Origin", "chrome-extension://jdiccldimpdaibmpdkjnbmckianbfold" },
        { "Sec-WebSocket-Protocol", "synthesize" },
        { "Sec-WebSocket-Version", "13" },
        { "User-Agent", $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{ChromiumMajorVersion}.0.0.0 Safari/537.36 Edg/{ChromiumMajorVersion}.0.0.0" },
        { "Accept-Encoding", "gzip, deflate, br" },
        { "Accept-Language", "en-US,en;q=0.9" }
    };

    public static readonly Dictionary<string, string> VoiceHeaders = new()
    {
        { "Authority", "speech.platform.bing.com" },
        { "Sec-CH-UA", $"\"Not/A) Brand\";v=\"8\", \"Chromium\";v=\"{ChromiumMajorVersion}\", \"Microsoft Edge\";v=\"{ChromiumMajorVersion}\"" },
        { "Sec-CH-UA-Mobile", "?0" },
        { "Accept", "*/*" },
        { "Sec-Fetch-Site", "none" },
        { "Sec-Fetch-Mode", "cors" },
        { "Sec-Fetch-Dest", "empty" },
        { "User-Agent", $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{ChromiumMajorVersion}.0.0.0 Safari/537.36 Edg/{ChromiumMajorVersion}.0.0.0" },
        { "Accept-Encoding", "gzip, deflate, br" },
        { "Accept-Language", "en-US,en;q=0.9" }
    };
}