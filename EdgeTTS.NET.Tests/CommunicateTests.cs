using Xunit;
using System.Reflection;

namespace EdgeTTS.NET.Tests;

public class CommunicateTests
{
    [Fact]
    public void RemoveIncompatibleCharacters_ShouldReplaceForbiddenChars()
    {
        // Using reflection to test private static method or move it to a helper if needed.
        // For now, let's assume we can test it if it was internal/public or use reflection.
        var type = typeof(Communicate);
        var method = type.GetMethod("RemoveIncompatibleCharacters", BindingFlags.NonPublic | BindingFlags.Static);
        
        var input = "Line1" + (char)11 + "Line2" + (char)7 + "Line3"; // VT and BEL
        var result = (string?)method?.Invoke(null, new object[] { input });

        Assert.Equal("Line1 Line2 Line3", result);
    }
}
