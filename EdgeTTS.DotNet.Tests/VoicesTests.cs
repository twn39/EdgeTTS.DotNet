using Xunit;
using Xunit.Abstractions;

namespace EdgeTTS.DotNet.Tests;

public class VoicesTests
{
    private readonly ITestOutputHelper _output;

    public VoicesTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task ListVoices_ShouldPrintAllVoices()
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
}
