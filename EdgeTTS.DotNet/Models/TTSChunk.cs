namespace EdgeTTS.DotNet.Models;


public abstract record TTSChunk(string Type);

public record AudioChunk(byte[] Data) : TTSChunk("audio");

public record MetadataChunk(
    string MetadataType, 
    TimeSpan Offset, 
    TimeSpan Duration, 
    string Text) : TTSChunk(MetadataType);

public record TurnEndChunk() : TTSChunk("turn.end");

public record UnknownChunk(string RawData) : TTSChunk("unknown");