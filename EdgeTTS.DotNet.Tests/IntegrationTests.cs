using Xunit;
using Xunit.Abstractions;
using EdgeTTS.DotNet;
using EdgeTTS.DotNet.Models;

namespace EdgeTTS.DotNet.Tests;

[Collection("WebSocket")]
[Trait("Category", "Integration")]
public class IntegrationTests
{
    private readonly ITestOutputHelper _output;

    public IntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task ListVoices_ShouldReturnNonEmptyList()
    {
        var voices = await Voices.ListVoicesAsync();
        Assert.NotNull(voices);
        Assert.NotEmpty(voices);
        _output.WriteLine($"Total voices: {voices.Count}");
    }

    [Fact]
    public async Task ListVoices_ShouldContainExpectedVoices()
    {
        var voices = await Voices.ListVoicesAsync();
        
        // Verify some well-known voices exist
        Assert.Contains(voices, v => v.ShortName == "en-US-AriaNeural");
        Assert.Contains(voices, v => v.ShortName == "zh-CN-XiaoxiaoNeural");
    }

    [Fact]
    public async Task ListVoices_ShouldHaveVoiceTagData()
    {
        var voices = await Voices.ListVoicesAsync();

        var ariaVoice = voices.FirstOrDefault(v => v.ShortName == "en-US-AriaNeural");
        Assert.NotNull(ariaVoice);
        Assert.NotNull(ariaVoice.VoiceTag);
        Assert.NotEmpty(ariaVoice.VoiceTag.VoicePersonalities);

        _output.WriteLine($"Voice: {ariaVoice.ShortName}");
        _output.WriteLine($"  Categories: {string.Join(", ", ariaVoice.VoiceTag.ContentCategories)}");
        _output.WriteLine($"  Personalities: {string.Join(", ", ariaVoice.VoiceTag.VoicePersonalities)}");
    }

    [Fact]
    public async Task ListVoices_ShouldHaveRequiredFields()
    {
        var voices = await Voices.ListVoicesAsync();
        
        foreach (var voice in voices.Take(5))
        {
            Assert.False(string.IsNullOrEmpty(voice.Name), "Voice.Name should not be empty");
            Assert.False(string.IsNullOrEmpty(voice.ShortName), "Voice.ShortName should not be empty");
            Assert.False(string.IsNullOrEmpty(voice.Gender), "Voice.Gender should not be empty");
            Assert.False(string.IsNullOrEmpty(voice.Locale), "Voice.Locale should not be empty");
        }
    }

    [Fact]
    public async Task StreamAsync_WithWordBoundary_ShouldProduceSubtitles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"edgetts_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var srtPath = Path.Combine(tempDir, "hello.srt");

        try
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
                    _output.WriteLine($"[{metadata.Type}] {metadata.Offset:hh\\:mm\\:ss\\.fff} : {metadata.Text}");
                }
            }

            Assert.True(audioBytes.Count > 0, "Should receive audio data");

            string srt = subMaker.GetSrt();
            Assert.NotEmpty(srt);
            await File.WriteAllTextAsync(srtPath, srt);

            // Assert SRT structure
            Assert.Contains("00:00:00", srt);
            Assert.Contains("Hello", srt);
            Assert.Contains("world", srt);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task StreamAsync_WithSentenceBoundary_ShouldWork()
    {
        var communicate = new Communicate("This is a test.", boundaryType: "SentenceBoundary");
        var audioBytes = new List<byte>();
        var gotSentenceBoundary = false;

        await foreach (var chunk in communicate.StreamAsync())
        {
            if (chunk is AudioChunk audio)
            {
                audioBytes.AddRange(audio.Data);
            }
            else if (chunk is MetadataChunk metadata)
            {
                Assert.Equal("SentenceBoundary", metadata.Type);
                gotSentenceBoundary = true;
                _output.WriteLine($"[Sentence] {metadata.Text}");
            }
        }

        Assert.True(audioBytes.Count > 0, "Should receive audio data");
        Assert.True(gotSentenceBoundary, "Should receive SentenceBoundary metadata");
    }

    [Fact]
    public async Task StreamAsync_WithChineseText_ShouldWork()
    {
        var communicate = new Communicate("你好世界", voice: "zh-CN-XiaoxiaoNeural");
        var audioBytes = new List<byte>();

        await foreach (var chunk in communicate.StreamAsync())
        {
            if (chunk is AudioChunk audio)
            {
                audioBytes.AddRange(audio.Data);
            }
        }

        Assert.True(audioBytes.Count > 0, "Should receive audio for Chinese text");
    }
}
