namespace EdgeTTS.DotNet.Models;

using System.Text.RegularExpressions;


/// <summary>
/// Represents the internal TTS configuration.
/// </summary>
public record TTSConfig
{
    public string Voice { get; }
    public string Rate { get; }
    public string Volume { get; }
    public string Pitch { get; }
    public string BoundaryType { get; }

    public TTSConfig(string? voice = null, string rate = "+0%", string volume = "+0%", string pitch = "+0Hz", string boundaryType = "SentenceBoundary")
    {
        Voice = ValidateVoice(voice ?? Constants.DefaultVoice);
        Rate = ValidateStringParam(nameof(rate), rate, @"^[+-]\d+%$");
        Volume = ValidateStringParam(nameof(volume), volume, @"^[+-]\d+%$");
        Pitch = ValidateStringParam(nameof(pitch), pitch, @"^[+-]\d+Hz$");
        BoundaryType = boundaryType == "WordBoundary" ? "WordBoundary" : "SentenceBoundary";
    }

    private static string ValidateStringParam(string paramName, string paramValue, string pattern)
    {
        if (!Regex.IsMatch(paramValue, pattern))
        {
            throw new ArgumentException($"Invalid {paramName} '{paramValue}'.");
        }
        return paramValue;
    }

    private static string ValidateVoice(string voice)
    {
        var match = Regex.Match(voice, @"^([a-z]{2,})-([A-Z]{2,})-(.+Neural)$");
        if (match.Success)
        {
            var lang = match.Groups[1].Value;
            var region = match.Groups[2].Value;
            var name = match.Groups[3].Value;
            return $"Microsoft Server Speech Text to Speech Voice ({lang}-{region}, {name})";
        }

        // Also validate the full format
        if (!Regex.IsMatch(voice, @"^Microsoft Server Speech Text to Speech Voice \(.+,.+\)$"))
        {
            throw new ArgumentException($"Invalid voice format '{voice}'.");
        }

        return voice;
    }
}