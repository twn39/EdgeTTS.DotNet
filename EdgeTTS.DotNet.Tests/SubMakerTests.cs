using EdgeTTS.DotNet.Models;
using Xunit;

namespace EdgeTTS.DotNet.Tests;

public class SubMakerTests
{
    [Fact]
    public void FeedAndGetSrt_ShouldGenerateCorrectFormat()
    {
        var subMaker = new SubMaker();
        var chunk1 = new MetadataChunk("WordBoundary", TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.5), "Hello");
        var chunk2 = new MetadataChunk("WordBoundary", TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1), "World");

        subMaker.Feed(chunk1);
        subMaker.Feed(chunk2);

        var srt = subMaker.GetSrt();
        
        var expected = "1\n00:00:01,000 --> 00:00:01,500\nHello\n\n2\n00:00:02,000 --> 00:00:03,000\nWorld\n";
        Assert.Equal(expected, srt);
    }

    [Fact]
    public void Feed_WithMixedTypes_ShouldThrowArgumentException()
    {
        var subMaker = new SubMaker();
        subMaker.Feed(new MetadataChunk("WordBoundary", TimeSpan.Zero, TimeSpan.Zero, ""));
        
        Assert.Throws<ArgumentException>(() => subMaker.Feed(new MetadataChunk("SentenceBoundary", TimeSpan.Zero, TimeSpan.Zero, "")));
    }

    [Fact]
    public void Feed_WithInvalidType_ShouldThrowArgumentException()
    {
        var subMaker = new SubMaker();
        Assert.Throws<ArgumentException>(() =>
            subMaker.Feed(new MetadataChunk("InvalidType", TimeSpan.Zero, TimeSpan.Zero, "test")));
    }

    [Fact]
    public void Feed_SentenceBoundary_ShouldWork()
    {
        var subMaker = new SubMaker();
        subMaker.Feed(new MetadataChunk("SentenceBoundary", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(2), "Hello world!"));

        var srt = subMaker.GetSrt();
        Assert.Contains("Hello world!", srt);
        Assert.Contains("00:00:00,000 --> 00:00:02,000", srt);
    }

    [Fact]
    public void GetSrt_WithNoFeeds_ShouldReturnMinimalString()
    {
        var subMaker = new SubMaker();
        var srt = subMaker.GetSrt();
        Assert.Equal("\n", srt);
    }

    [Fact]
    public void ToString_ShouldReturnSameAsGetSrt()
    {
        var subMaker = new SubMaker();
        subMaker.Feed(new MetadataChunk("WordBoundary", TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.5), "Test"));

        Assert.Equal(subMaker.GetSrt(), subMaker.ToString());
    }

    [Fact]
    public void Feed_WithMillisecondPrecision_ShouldFormatCorrectly()
    {
        var subMaker = new SubMaker();
        subMaker.Feed(new MetadataChunk(
            "WordBoundary",
            new TimeSpan(0, 1, 23, 4, 567),  // 01:23:04.567
            TimeSpan.FromMilliseconds(100),
            "Precise"));

        var srt = subMaker.GetSrt();
        Assert.Contains("01:23:04,567 --> 01:23:04,667", srt);
    }
}
