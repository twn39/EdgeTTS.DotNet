# EdgeTTS.DotNet

[![NuGet Version](https://img.shields.io/nuget/v/EdgeTTS.DotNet?style=flat&logo=nuget)](https://www.nuget.org/packages/EdgeTTS.DotNet)
[![NuGet Downloads](https://img.shields.io/nuget/dt/EdgeTTS.DotNet?style=flat&logo=nuget)](https://www.nuget.org/packages/EdgeTTS.DotNet)
[![Build Status](https://img.shields.io/github/actions/workflow/status/twn39/EdgeTTS.DotNet/test.yml?branch=main&style=flat&logo=github)](https://github.com/twn39/EdgeTTS.DotNet/actions/workflows/test.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple?style=flat&logo=.net)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20macOS%20%7C%20Linux-lightgrey?style=flat)](https://github.com/twn39/EdgeTTS.DotNet)

`EdgeTTS.DotNet` is a C# (.NET) library that allows you to use Microsoft Edge's online text-to-speech service. It is a feature-complete migration of the popular Python [edge-tts](https://github.com/rany2/edge-tts) library, designed for performance, cross-platform compatibility, and ease of use.

## Features

- **High-Quality Speech**: Access Microsoft Edge's neural TTS voices for natural-sounding speech.
- **Multilingual Support**: Supports over 400 voices across numerous languages and regions.
- **Subtitles**: Generate SRT formatted subtitles from `WordBoundary` or `SentenceBoundary` events.
- **Customizable Prosody**: Adjust speech rate, volume, and pitch to suit your needs.
- **Cross-Platform**: Built with .NET 10.0, fully compatible with Windows, macOS, Linux, and mobile platforms like **.NET MAUI**.
- **Robustness**: Includes built-in clock skew correction (DRM), automatic 403 retry, WebSocket compression, and comprehensive error handling.
- **Voice Metadata**: Access voice tags including `ContentCategories` and `VoicePersonalities`.

## Installation

```bash
dotnet add package EdgeTTS.DotNet
```

## Quick Start

### Basic Usage

Save text to an MP3 file:

```csharp
using EdgeTTS.DotNet;

var request = new Communicate("Hello, world!", voice: "en-US-EmmaMultilingualNeural");
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
    Console.WriteLine($"  Categories: {string.Join(", ", voice.VoiceTag.ContentCategories)}");
    Console.WriteLine($"  Personalities: {string.Join(", ", voice.VoiceTag.VoicePersonalities)}");
}
```

## Advanced Options

| Option | Description | Format |
|---|---|---|
| `voice` | The short name of the voice (e.g., `en-US-EmmaMultilingualNeural`) | String |
| `rate` | The speed of the speech | `+0%`, `-50%`, etc. |
| `volume` | The volume of the speech | `+0%`, `-25%`, etc. |
| `pitch` | The pitch of the speech | `+0Hz`, `-5Hz`, etc. |
| `boundaryType` | Type of metadata events to receive | `SentenceBoundary` (default) or `WordBoundary` |
| `proxy` | Proxy server for network requests | `http://proxy:port` |

## Error Handling

The library provides specific exception types for robust error handling:

| Exception | Description |
|---|---|
| `NoAudioReceivedException` | No audio was received from the service |
| `UnexpectedResponseException` | Unexpected response format from the server |
| `UnknownResponseException` | Unknown response type received |
| `WebSocketException` | WebSocket connection or communication error |
| `SkewAdjustmentException` | Clock skew correction failed (missing server date) |

## Testing

The project includes a comprehensive test suite using xUnit:

```bash
dotnet test
```

## License

This project is licensed under the MIT License. See the Python project [edge-tts](https://github.com/rany2/edge-tts) for more details on the original implementation.
