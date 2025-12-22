namespace EdgeTTS.DotNet;

using System.Text;
using Models;

public class SubMaker
{
    private readonly List<Subtitle> _cues = new();
    private string? _type;

    public void Feed(MetadataChunk msg)
    {
        if (msg.Type != "WordBoundary" && msg.Type != "SentenceBoundary")
        {
            throw new ArgumentException("Invalid message type, expected 'WordBoundary' or 'SentenceBoundary'.");
        }

        if (_type == null)
        {
            _type = msg.Type;
        }
        else if (_type != msg.Type)
        {
            throw new ArgumentException($"Expected message type '{_type}', but got '{msg.Type}'.");
        }

        _cues.Add(new Subtitle(
            Index: _cues.Count + 1,
            Start: msg.Offset,
            End: msg.Offset + msg.Duration,
            Content: msg.Text
        ));
    }

    public string GetSrt()
    {
        var sb = new StringBuilder();
        foreach (var cue in _cues)
        {
            sb.AppendLine(cue.Index.ToString());
            sb.AppendLine($"{FormatTime(cue.Start)} --> {FormatTime(cue.End)}");
            sb.AppendLine(cue.Content);
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd() + "\n";
    }

    private static string FormatTime(TimeSpan ts)
    {
        return $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2},{ts.Milliseconds:D3}";
    }

    public override string ToString() => GetSrt();
}
