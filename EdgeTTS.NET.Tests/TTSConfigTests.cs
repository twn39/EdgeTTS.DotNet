using EdgeTTS.NET.Models;
using Xunit;

namespace EdgeTTS.NET.Tests;

public class TTSConfigTests
{
    [Fact]
    public void Constructor_WithValidParams_ShouldReturnCorrectValues()
    {
        var config = new TTSConfig("en-US-AriaNeural", "+10%", "+20%", "+5Hz", "WordBoundary");
        
        Assert.Equal("Microsoft Server Speech Text to Speech Voice (en-US, AriaNeural)", config.Voice);
        Assert.Equal("+10%", config.Rate);
        Assert.Equal("+20%", config.Volume);
        Assert.Equal("+5Hz", config.Pitch);
        Assert.Equal("WordBoundary", config.BoundaryType);
    }

    [Fact]
    public void Constructor_WithDefaultParams_ShouldUseDefaults()
    {
        var config = new TTSConfig();
        
        Assert.Contains("EmmaMultilingualNeural", config.Voice);
        Assert.Equal("+0%", config.Rate);
        Assert.Equal("+0%", config.Volume);
        Assert.Equal("+0Hz", config.Pitch);
        Assert.Equal("SentenceBoundary", config.BoundaryType);
    }

    [Theory]
    [InlineData("rate", "10%")]
    [InlineData("rate", "+10")]
    [InlineData("volume", "20%")]
    [InlineData("pitch", "5Hz")]
    [InlineData("pitch", "+5Hzz")]
    public void Constructor_WithInvalidParams_ShouldThrowArgumentException(string param, string value)
    {
        Assert.Throws<ArgumentException>(() => 
            param switch
            {
                "rate" => new TTSConfig(rate: value),
                "volume" => new TTSConfig(volume: value),
                "pitch" => new TTSConfig(pitch: value),
                _ => throw new Exception()
            });
    }

    [Fact]
    public void Constructor_WithInvalidVoice_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new TTSConfig(voice: "InvalidVoice"));
    }
}
