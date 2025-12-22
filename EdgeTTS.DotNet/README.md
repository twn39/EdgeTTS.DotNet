# EdgeTTS.DotNet

`EdgeTTS.DotNet` is a C# (.NET) library that allows you to use Microsoft Edge's online text-to-speech service. It is a feature-complete migration of the popular Python [edge-tts](https://github.com/rany2/edge-tts) library, designed for performance, cross-platform compatibility, and ease of use.

## Features

- **High-Quality Speech**: Access Microsoft Edge's neural TTS voices for natural-sounding speech.
- **Multilingual Support**: Supports over 300 voices across numerous languages and regions.
- **Subtitles**: Generate SRT formatted subtitles from `WordBoundary` or `SentenceBoundary` events.
- **Customizable Prosody**: Adjust speech rate, volume, and pitch to suit your needs.
- **Cross-Platform**: Built with .NET 9.0, fully compatible with Windows, macOS, Linux, and mobile platforms like **.NET MAUI**.
- **Robustness**: Includes built-in clock skew correction (DRM) and comprehensive error handling.

## Installation

You can reference the `EdgeTTS.DotNet` project in your solution:

```bash
dotnet add reference path/to/EdgeTTS.DotNet/EdgeTTS.DotNet.csproj
```

## Quick Start

### Basic Usage

Save text to an MP3 file:

```csharp
using EdgeTTS.DotNet;

var request = new Communicate("Hello, world!", voice: "en-US-AriaNeural");
await request.SaveAsync("hello.mp3");
```

### Generating Subtitles

Use the `SubMaker` class to create subtitles:

```csharp
using EdgeTTS.DotNet;
using EdgeTTS.DotNet.Models;

var communicate = new Communicate("Hello world!", rate: "+10%", boundaryType: "WordBoundary");
var subMaker = new SubMaker();

await foreach (var chunk in communicate.StreamAsync())
{
    if (chunk is AudioChunk audio)
    {
        // Handle audio data
    }
    else if (chunk is MetadataChunk metadata)
    {
        subMaker.Feed(metadata);
    }
}

string srt = subMaker.GetSrt();
File.WriteAllText("hello.srt", srt);
```

### Listing Available Voices

```csharp
using EdgeTTS.DotNet;

var voices = await Voices.ListVoicesAsync();
foreach (var voice in voices)
{
    Console.WriteLine($"{voice.ShortName} ({voice.Gender}) - {voice.Locale}");
}
```

## Advanced Options

| Option | Description | Format |
|---|---|---|
| `voice` | The short name of the voice (e.g., `en-US-AriaNeural`) | String |
| `rate` | The speed of the speech | `+0%`, `-50%`, etc. |
| `volume` | The volume of the speech | `+0%`, `-25%`, etc. |
| `pitch` | The pitch of the speech | `+0Hz`, `-5Hz`, etc. |
| `boundaryType` | Type of metadata events to receive | `SentenceBoundary` (default) or `WordBoundary` |

## Unit Testing

The project includes a comprehensive test suite using xUnit. To run the tests, execute the following command from the root directory:

```bash
dotnet test
```

## License

This project is licensed under the MIT License. See the Python project [edge-tts](https://github.com/rany2/edge-tts) for more details on the original implementation.
