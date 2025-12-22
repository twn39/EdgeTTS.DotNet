namespace EdgeTTS.NET;

using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Net;
using Models;
using Text;



public class Communicate
{
    private readonly TTSConfig _config;
    private readonly IEnumerable<string> _textParts;
    private readonly string? _proxy;

    public Communicate(string text, string? voice = null, string rate = "+0%", string volume = "+0%", string pitch = "+0Hz", string boundaryType = "SentenceBoundary", string? proxy = null)
    {
        _config = new TTSConfig(voice, rate, volume, pitch, boundaryType);
        _proxy = proxy;

        // Clean text, escape, and then split
        var cleanedText = RemoveIncompatibleCharacters(text);
        var escapedText = WebUtility.HtmlEncode(cleanedText);
        _textParts = TextSplitter.SplitTextByByteLength(escapedText, 4096);
    }

    private static string RemoveIncompatibleCharacters(string text)
    {
        var chars = text.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            int code = chars[i];
            if ((code >= 0 && code <= 8) || (code >= 11 && code <= 12) || (code >= 14 && code <= 31))
            {
                chars[i] = ' ';
            }
        }
        return new string(chars);
    }

    
    /// <summary>
    /// Streams audio and metadata from the service.
    /// </summary>
    /// <returns>An asynchronous stream of TTSChunk objects.</returns>
    public async IAsyncEnumerable<TTSChunk> StreamAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var offsetCompensation = TimeSpan.Zero;
        var lastDurationOffset = TimeSpan.Zero;

        foreach (var part in _textParts)
        {
            if (cancellationToken.IsCancellationRequested) break;

            using var webSocket = new ClientWebSocket();
            foreach (var header in Constants.WssHeaders)
            {
                webSocket.Options.SetRequestHeader(header.Key, header.Value);
            }
            webSocket.Options.SetRequestHeader("Cookie", $"muid={Drm.GenerateMuid()};");


            var connectId = Guid.NewGuid().ToString("N");
            var wssUrl = $"{Constants.WssUrl}&ConnectionId={connectId}&Sec-MS-GEC={Drm.GenerateSecMsGec()}&Sec-MS-GEC-Version={Constants.SecMsGecVersion}";
            await webSocket.ConnectAsync(new Uri(wssUrl), cancellationToken);

            // Send speech config
            var wordBoundaryEnabled = _config.BoundaryType == "WordBoundary" ? "true" : "false";
            var sentenceBoundaryEnabled = _config.BoundaryType == "SentenceBoundary" ? "true" : "false";

            var speechConfig = "{\"context\":{\"synthesis\":{\"audio\":{\"metadataoptions\":{\"sentenceBoundaryEnabled\":\"" + sentenceBoundaryEnabled + "\",\"wordBoundaryEnabled\":\"" + wordBoundaryEnabled + "\"},\"outputFormat\":\"audio-24khz-48kbitrate-mono-mp3\"}}}}";
            var speechConfigMessage = $"X-Timestamp:{GetDateString()}\r\nContent-Type:application/json; charset=utf-8\r\nPath:speech.config\r\n\r\n{speechConfig}\r\n";
            await SendMessageAsync(webSocket, speechConfigMessage, cancellationToken);

            // Send SSML
            var ssml = CreateSsml(part);
            var requestId = Guid.NewGuid().ToString("N");
            var ssmlMessage = $"X-RequestId:{requestId}\r\nContent-Type:application/ssml+xml\r\nX-Timestamp:{GetDateString()}Z\r\nPath:ssml\r\n\r\n{ssml}";
            await SendMessageAsync(webSocket, ssmlMessage, cancellationToken);

            var audioWasReceivedInThisTurn = false;
            while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var (headers, data) = await ReceiveMessageAsync(webSocket, cancellationToken);
                if (headers is null)
                {
                    break; 
                }

                if (headers.TryGetValue("Path", out var path))
                {
                    if (path == "audio.metadata")
                    {
                        var metadata = ParseMetadata(data, offsetCompensation);
                        if (metadata != null)
                        {
                            lastDurationOffset = metadata.Offset + metadata.Duration;
                            yield return metadata;
                        }
                    }
                    else if (path == "turn.end")
                    {
                        offsetCompensation = lastDurationOffset + TimeSpan.FromMilliseconds(875);
                        yield return new TurnEndChunk();
                        break;
                    }
                    else if (path == "audio") // Directly handle Path: audio
                    {
                        // If Content-Type is present, it must be audio/mpeg.
                        // If Content-Type is missing AND data is empty, then continue (skip).
                        headers.TryGetValue("Content-Type", out var contentType);

                        if (string.IsNullOrEmpty(contentType)) // Content-Type is None
                        {
                            if (data.Length == 0)
                            {
                                continue; // Skip this message
                            }
                        }
                        else if (!contentType.StartsWith("audio/mpeg", StringComparison.OrdinalIgnoreCase))
                        {
                            continue; // Skip this message
                        }

                        // If we reach here, it's a valid audio chunk (either with audio/mpeg or no Content-Type but data)
                        audioWasReceivedInThisTurn = true;
                        yield return new AudioChunk(data);
                    }
                    // For other paths like "response", "turn.start", we just ignore them for yielding.
                }
                else
                {
                    // If no Path header, it's likely a binary audio message without a Path header (which is also possible)
                    // In this case, we must rely on Content-Type.
                    if (headers.TryGetValue("Content-Type", out var contentType) && contentType.StartsWith("audio/mpeg", StringComparison.OrdinalIgnoreCase))
                    {
                        audioWasReceivedInThisTurn = true;
                        yield return new AudioChunk(data);
                    }
                }
            }

            if (!audioWasReceivedInThisTurn && !cancellationToken.IsCancellationRequested)
            {
                throw new NoAudioReceivedException("No audio was received for the current text part. This might indicate an issue or an empty audio segment.");
            }
        }
    }
    
    public async Task SaveAsync(string audioFilePath, CancellationToken cancellationToken = default)
    {
        await using var fileStream = new FileStream(audioFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await foreach (var chunk in StreamAsync(cancellationToken))
        {
            if (chunk is AudioChunk audioChunk)
            {
                await fileStream.WriteAsync(audioChunk.Data, cancellationToken);
            }
        }
    }

    private string CreateSsml(string text)
    {
        return "<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'>" +
               $"<voice name='{_config.Voice}'>" +
               $"<prosody pitch='{_config.Pitch}' rate='{_config.Rate}' volume='{_config.Volume}'>" +
               text +
               "</prosody>" +
               "</voice>" +
               "</speak>";
    }

    private static string GetDateString()
    {
        return DateTime.UtcNow.ToString("ddd MMM dd yyyy HH:mm:ss 'GMT+0000 (Coordinated Universal Time)'", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task SendMessageAsync(ClientWebSocket webSocket, string message, CancellationToken cancellationToken)
    {
        var messageBytes = Encoding.UTF8.GetBytes(message);
        await webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, cancellationToken);
    }

    private async Task<(Dictionary<string, string>? headers, byte[] data)> ReceiveMessageAsync(ClientWebSocket webSocket, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        var buffer = new byte[1024 * 8]; // 8KB buffer
        WebSocketReceiveResult result;

        try
        {
            do
            {
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    return (null, Array.Empty<byte>());
                }
                memoryStream.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);
        }
        catch (System.Net.WebSockets.WebSocketException ex)
        {
            throw new EdgeTTS.NET.WebSocketException("WebSocket receive error", ex);
        }

        memoryStream.Seek(0, SeekOrigin.Begin);
        var messageBytes = memoryStream.ToArray();

        if (result.MessageType == WebSocketMessageType.Binary)
        {
            if (messageBytes.Length < 2)
            {
                throw new UnexpectedResponseException("Binary message too short to contain header length.");
            }

            var headerLength = (messageBytes[0] << 8) | messageBytes[1]; // Big-endian

            if (headerLength > messageBytes.Length - 2)
            {
                throw new UnexpectedResponseException($"Header length ({headerLength}) is greater than remaining data length ({messageBytes.Length - 2}).");
            }

            var headerData = messageBytes.AsSpan(2, headerLength);
            var bodyData = messageBytes.AsSpan(2 + headerLength).ToArray();

            var headersStr = Encoding.UTF8.GetString(headerData);
            var headers = ParseHeaders(headersStr);

            return (headers, bodyData);
        }
        else if (result.MessageType == WebSocketMessageType.Text)
        {
            var text = Encoding.UTF8.GetString(messageBytes);
            var separator = "\r\n\r\n";
            var separatorIndex = text.IndexOf(separator, StringComparison.Ordinal);

            if (separatorIndex == -1)
            {
                return (new Dictionary<string, string>(), messageBytes);
            }

            var headersStr = text.Substring(0, separatorIndex);
            var bodyStr = text.Substring(separatorIndex + separator.Length);

            var headers = ParseHeaders(headersStr);
            var bodyData = Encoding.UTF8.GetBytes(bodyStr);

            return (headers, bodyData);
        }

        throw new UnknownResponseException($"Received unhandled message type: {result.MessageType}");
    }

    private static Dictionary<string, string> ParseHeaders(string headersStr)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var lines = headersStr.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var parts = line.Split(':', 2);
            if (parts.Length == 2)
            {
                headers[parts[0].Trim()] = parts[1].Trim();
            }
        }
        return headers;
    }

    private static MetadataChunk? ParseMetadata(byte[] data, TimeSpan offsetCompensation)
    {
        try
        {
            var json = JsonNode.Parse(Encoding.UTF8.GetString(data));
            var metadataArray = json?["Metadata"]?.AsArray();
            if (metadataArray == null) return null;

            foreach (var metaObj in metadataArray)
            {
                var type = metaObj?["Type"]?.GetValue<string>();
                if (type == "WordBoundary" || type == "SentenceBoundary")
                {
                    var offsetTicks = metaObj?["Data"]?["Offset"]?.GetValue<long>() ?? 0;
                    var durationTicks = metaObj?["Data"]?["Duration"]?.GetValue<long>() ?? 0;
                    var text = metaObj?["Data"]?["text"]?["Text"]?.GetValue<string>() ?? "";

                    // Python's offset and duration are in 100-nanosecond units (ticks)
                    // C# TimeSpan.FromTicks expects 100-nanosecond units.
                    var offset = TimeSpan.FromTicks(offsetTicks) + offsetCompensation;
                    var duration = TimeSpan.FromTicks(durationTicks);

                    return new MetadataChunk(type, offset, duration, WebUtility.HtmlDecode(text));
                }
                else if (type == "SessionEnd")
                {
                    continue;
                }
                else
                {
                    throw new UnknownResponseException($"Unknown metadata type: {type}");
                }
            }
        }
        catch (JsonException ex)
        {
        }
        catch (Exception ex)
        {
        }
        return null;
    }
}