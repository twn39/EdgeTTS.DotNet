using Xunit;

namespace EdgeTTS.DotNet.Tests;

public class DrmTests
{
    [Fact]
    public void GenerateSecMsGec_ShouldReturnUppercaseHexString()
    {
        var gec = Drm.GenerateSecMsGec();

        Assert.NotNull(gec);
        Assert.NotEmpty(gec);
        // SHA256 hex digest is 64 characters
        Assert.Equal(64, gec.Length);
        // Should be uppercase hex
        Assert.Matches("^[A-F0-9]{64}$", gec);
    }

    [Fact]
    public void GenerateSecMsGec_ShouldBeConsistentWithinSameWindow()
    {
        // Two calls within the same 5-minute window should return the same GEC
        var gec1 = Drm.GenerateSecMsGec();
        var gec2 = Drm.GenerateSecMsGec();

        Assert.Equal(gec1, gec2);
    }

    [Fact]
    public void GenerateMuid_ShouldReturnUppercaseHex32Chars()
    {
        var muid = Drm.GenerateMuid();

        Assert.NotNull(muid);
        Assert.Equal(32, muid.Length);
        Assert.Matches("^[A-F0-9]{32}$", muid);
    }

    [Fact]
    public void GenerateMuid_ShouldBeUnique()
    {
        var muid1 = Drm.GenerateMuid();
        var muid2 = Drm.GenerateMuid();

        Assert.NotEqual(muid1, muid2);
    }

    [Fact]
    public void HandleClientResponseError_WithEmptyDictionary_ShouldThrowSkewAdjustmentException()
    {
        var headers = new Dictionary<string, string>();

        Assert.Throws<SkewAdjustmentException>(() => Drm.HandleClientResponseError(headers));
    }

    [Fact]
    public void HandleClientResponseError_WithInvalidDate_ShouldThrowSkewAdjustmentException()
    {
        var headers = new Dictionary<string, string>
        {
            { "Date", "not-a-valid-date" }
        };

        Assert.Throws<SkewAdjustmentException>(() => Drm.HandleClientResponseError(headers));
    }

    [Fact]
    public void HandleClientResponseError_WithValidDate_ShouldNotThrow()
    {
        var headers = new Dictionary<string, string>
        {
            { "Date", DateTime.UtcNow.ToString("R") }
        };

        // Should not throw — adjusts clock skew silently
        var exception = Record.Exception(() => Drm.HandleClientResponseError(headers));
        Assert.Null(exception);
    }
}
