using Xunit;
using System.Reflection;

namespace EdgeTTS.DotNet.Tests;

[Collection("WebSocket")]
public class CommunicateTests
{
    [Fact]
    public void RemoveIncompatibleCharacters_ShouldReplaceForbiddenChars()
    {
        var type = typeof(Communicate);
        var method = type.GetMethod("RemoveIncompatibleCharacters", BindingFlags.NonPublic | BindingFlags.Static);
        
        var input = "Line1" + (char)11 + "Line2" + (char)7 + "Line3"; // VT and BEL
        var result = (string?)method?.Invoke(null, new object[] { input });

        Assert.Equal("Line1 Line2 Line3", result);
    }

    [Fact]
    public void RemoveIncompatibleCharacters_ShouldKeepNormalCharacters()
    {
        var type = typeof(Communicate);
        var method = type.GetMethod("RemoveIncompatibleCharacters", BindingFlags.NonPublic | BindingFlags.Static);

        var input = "Hello, World! 你好世界 123\n\ttab";
        var result = (string?)method?.Invoke(null, new object[] { input });

        Assert.Equal(input, result);
    }

    [Fact]
    public void Constructor_ShouldAcceptDefaultVoice()
    {
        var communicate = new Communicate("Hello");
        Assert.NotNull(communicate);
    }

    [Fact]
    public void Constructor_ShouldAcceptCustomVoice()
    {
        var communicate = new Communicate("Test", voice: "zh-CN-XiaoxiaoNeural", rate: "+10%", pitch: "-5Hz");
        Assert.NotNull(communicate);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SaveAsync_ShouldSaveAudioFileToTempDir()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"edgetts_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var filename = Path.Combine(tempDir, "hello.mp3");

        try
        {
            var request = new Communicate("Hello, world!", voice: "en-US-AriaNeural");
            await request.SaveAsync(filename);

            Assert.True(File.Exists(filename));
            var fileInfo = new FileInfo(filename);
            Assert.True(fileInfo.Length > 0, "Audio file should not be empty");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
