using EdgeTTS.NET.Text;
using Xunit;
using System.Text;

namespace EdgeTTS.NET.Tests;

public class TextSplitterTests
{
    [Fact]
    public void SplitTextByByteLength_ShouldSplitAtWhitespace()
    {
        var text = "Hello World From Edge TTS";
        var chunks = TextSplitter.SplitTextByByteLength(text, 12).ToList();

        Assert.Equal(3, chunks.Count);
        Assert.Equal("Hello World", chunks[0]);
        Assert.Equal("From Edge", chunks[1]);
        Assert.Equal("TTS", chunks[2]);
    }

    [Fact]
    public void SplitTextByByteLength_ShouldRespectUTF8()
    {
        var text = "你好世界，这是一个测试"; // Chinese characters are 3 bytes each in UTF-8
        // "你好" is 6 bytes. Total 12 chars.
        var chunks = TextSplitter.SplitTextByByteLength(text, 12).ToList();

        foreach (var chunk in chunks)
        {
            Assert.True(Encoding.UTF8.GetByteCount(chunk) <= 12);
            // Verify no broken characters
            Assert.Equal(chunk, Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(chunk)));
        }
    }

    [Fact]
    public void SplitTextByByteLength_ShouldNotSplitMidXmlEntity()
    {
        var text = "A_&amp;_B";
        // "A_&amp" is 5 bytes.
        // If we split at 5, it includes '&', 'a', 'm', 'p'.
        // The splitter should find it's splitting an entity and move back to before '&'.
        // Wait, if limit is 5, it fits '&amp;' (5 bytes).
        var chunks = TextSplitter.SplitTextByByteLength(text, 5).ToList();

        Assert.Equal("A_", chunks[0]);
        Assert.Equal("&amp;", chunks[1]);
        Assert.Equal("_B", chunks[2]);
    }
}
