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
}
