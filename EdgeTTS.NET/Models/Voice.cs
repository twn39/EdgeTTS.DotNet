namespace EdgeTTS.NET.Models;

using System.Text.Json.Serialization;


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
}