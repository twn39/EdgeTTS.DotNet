namespace EdgeTTS.NET.Models;

public record Subtitle(int? Index, TimeSpan Start, TimeSpan End, string Content);
