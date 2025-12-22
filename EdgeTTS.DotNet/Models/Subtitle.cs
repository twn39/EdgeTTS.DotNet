namespace EdgeTTS.DotNet.Models;

public record Subtitle(int? Index, TimeSpan Start, TimeSpan End, string Content);
