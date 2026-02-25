namespace EdgeTTS.DotNet.Models;

using System.Text.Json.Serialization;


public record VoiceTag
{
    [JsonPropertyName("ContentCategories")]
    public List<string> ContentCategories { get; init; } = new();

    [JsonPropertyName("VoicePersonalities")]
    public List<string> VoicePersonalities { get; init; } = new();
}

public record Voice
{
    [JsonPropertyName("Name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("ShortName")]
    public string ShortName { get; init; } = string.Empty;

    [JsonPropertyName("Gender")]
    public string Gender { get; init; } = string.Empty;

    [JsonPropertyName("Locale")]
    public string Locale { get; init; } = string.Empty;

    [JsonPropertyName("SuggestedCodec")]
    public string SuggestedCodec { get; init; } = string.Empty;

    [JsonPropertyName("FriendlyName")]
    public string FriendlyName { get; init; } = string.Empty;

    [JsonPropertyName("Status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("VoiceTag")]
    public VoiceTag VoiceTag { get; init; } = new();
}