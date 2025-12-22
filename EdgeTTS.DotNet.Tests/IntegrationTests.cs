using Xunit;
using Xunit.Abstractions;
using EdgeTTS.DotNet;
using EdgeTTS.DotNet.Models;

namespace EdgeTTS.DotNet.Tests;

public class IntegrationTests
{
    private readonly ITestOutputHelper _output;

    public IntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task ListVoices_IntegrationTest()
    {
        var voices = await Voices.ListVoicesAsync();
        Assert.NotNull(voices);
        Assert.NotEmpty(voices);

        foreach (var voice in voices)
        {
            var line = $"{voice.ShortName} ({voice.Gender}) - {voice.Locale}";
            _output.WriteLine(line);
            Console.WriteLine(line);
        }
    }

    [Fact]
    public async Task StreamAsync_SubtitleIntegrationTest()
    {
        var communicate = new Communicate("Hello world!", rate: "+10%", boundaryType: "WordBoundary");
        var subMaker = new SubMaker();
        var audioBytes = new List<byte>();

        await foreach (var chunk in communicate.StreamAsync())
        {
            if (chunk is AudioChunk audio)
            {
                audioBytes.AddRange(audio.Data);
            }
            else if (chunk is MetadataChunk metadata)
            {
                subMaker.Feed(metadata);
            }
        }

        string srt = subMaker.GetSrt();
        Assert.NotEmpty(srt);
        
        var srtPath = "hello.srt";
        await File.WriteAllTextAsync(srtPath, srt);
        _output.WriteLine($"SRT file written to {Path.GetFullPath(srtPath)}");
        
        // Assert some basic SRT structure
        Assert.Contains("00:00:00", srt);
        Assert.Contains("Hello", srt);
        Assert.Contains("world", srt);
    }
}
