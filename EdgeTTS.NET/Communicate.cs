namespace EdgeTTS.NET;

using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;
using Models;
using Text;



public class Communicate
{
    private readonly TTSConfig _config;
    private readonly IEnumerable<string> _textParts;
    private readonly string? _proxy;

    public Communicate(string text, string? voice = null, string rate = "+0%", string volume = "+0%", string pitch = "+0Hz", string? proxy = null)
    {
        _config = new TTSConfig(voice, rate, volume, pitch);
        _proxy = proxy;

        // Escape text and then split
        var escapedText = HttpUtility.HtmlEncode(text);
        _textParts = TextSplitter.SplitTextByByteLength(escapedText, 4096);
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

            if (!string.IsNullOrEmpty(_proxy))
            {
                Console.WriteLine($"[Warning] Explicit proxy '{_proxy}' provided, but ClientWebSocket's direct proxy support is limited. Relying on environment variables.");
            }

            var connectId = Guid.NewGuid().ToString("N");
            var wssUrl = $"{Constants.WssUrl}&ConnectionId={connectId}&Sec-MS-GEC={Drm.GenerateSecMsGec()}&Sec-MS-GEC-Version={Constants.SecMsGecVersion}";

            await webSocket.ConnectAsync(new Uri(wssUrl), cancellationToken);

            // Send speech config
            var speechConfig = "{\"context\":{\"synthesis\":{\"audio\":{\"metadataoptions\":{\"sentenceBoundaryEnabled\":\"false\",\"wordBoundaryEnabled\":\"true\"},\"outputFormat\":\"audio-24khz-48kbitrate-mono-mp3\"}}}}";
            var speechConfigMessage = $"X-Timestamp:{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}\r\nContent-Type:application/json; charset=utf-8\r\nPath:speech.config\r\n\r\n{speechConfig}";
            await SendMessageAsync(webSocket, speechConfigMessage, cancellationToken);

            // Send SSML
            var ssml = CreateSsml(part);
            var ssmlMessage = $"X-RequestId:{Guid.NewGuid():N}\r\nContent-Type:application/ssml+xml\r\nX-Timestamp:{DateTime.UtcNow:yyyy-MM:ddTHH:mm:ss.fffZ}Z\r\nPath:ssml\r\n\r\n{ssml}";
            await SendMessageAsync(webSocket, ssmlMessage, cancellationToken);

            var audioWasReceivedInThisTurn = false;
            while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var (headers, data) = await ReceiveMessageAsync(webSocket, cancellationToken);
                if (headers is null) break; // Connection closed or error

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
                            // If Content-Type is None but data is NOT empty, this is an unexpected response.
                            // Console.WriteLine("[WebSocket] Warning: Received audio message with no Content-Type but with data. Treating as audio/mpeg.");
                            // Fall through to yield it as audio.
                        }
                        else if (!contentType.StartsWith("audio/mpeg", StringComparison.OrdinalIgnoreCase))
                        {
                            // Console.WriteLine($"[WebSocket] Warning: Received audio message with unexpected Content-Type: {contentType}. Skipping.");
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
                        // Console.WriteLine($"[WebSocket] Matched Content-Type: {contentType}. Yielding audio chunk.");
                        audioWasReceivedInThisTurn = true;
                        yield return new AudioChunk(data);
                    }
                    //else
                    //{
                        //Console.WriteLine($"[WebSocket] Received message with unknown Path or Content-Type. Path: {headers.GetValueOrDefault("Path", "N/A")}, Content-Type: {headers.GetValueOrDefault("Content-Type", "N/A")}. Data length: {data.Length}");
                    //}
                }
            }

            if (!audioWasReceivedInThisTurn && !cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("[Warning] No audio was received for the current text part. This might indicate an issue or an empty audio segment.");
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

    private static async Task SendMessageAsync(ClientWebSocket webSocket, string message, CancellationToken cancellationToken)
    {
        var messageBytes = Encoding.UTF8.GetBytes(message);
        await webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, cancellationToken);
    }

    
     private static async Task<(Dictionary<string, string>? headers, byte[] data)> ReceiveMessageAsync(ClientWebSocket webSocket, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        var buffer = new byte[1024 * 4]; // 4KB buffer
        WebSocketReceiveResult result;

        do
        {
            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                return (null, Array.Empty<byte>());
            }
            memoryStream.Write(buffer, 0, result.Count);
        } while (!result.EndOfMessage);

        memoryStream.Seek(0, SeekOrigin.Begin);
        var messageBytes = memoryStream.ToArray();

        if (result.MessageType == WebSocketMessageType.Binary)
        {
            if (messageBytes.Length < 2)
            {
                //Console.WriteLine("[WebSocket-Receive] Warning: Binary message too short to contain header length.");
                return (new Dictionary<string, string>(), Array.Empty<byte>());
            }

            var headerLength = (messageBytes[0] << 8) | messageBytes[1]; // Big-endian

            if (headerLength > messageBytes.Length - 2)
            {
                //Console.WriteLine($"[WebSocket-Receive] Warning: Header length ({headerLength}) is greater than remaining data length ({messageBytes.Length - 2}).");
                return (new Dictionary<string, string>(), Array.Empty<byte>());
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
                // Simple text message without structured headers (e.g., "turn.start", "response" without full headers)
                // Python handles these as "path not in (b"response", b"turn.start")"
                // We'll return an empty header dictionary and the full text as data.
                return (new Dictionary<string, string>(), messageBytes);
            }

            var headersStr = text.Substring(0, separatorIndex);
            var bodyStr = text.Substring(separatorIndex + separator.Length);

            var headers = ParseHeaders(headersStr);
            var bodyData = Encoding.UTF8.GetBytes(bodyStr);

            return (headers, bodyData);
        }

        //Console.WriteLine($"[WebSocket-Receive] Warning: Received unhandled message type: {result.MessageType}");
        return (new Dictionary<string, string>(), Array.Empty<byte>());
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

                    return new MetadataChunk(type, offset, duration, HttpUtility.HtmlDecode(text));
                }
            }
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[ParseMetadata] Error parsing JSON: {ex.Message}. Raw data: {Encoding.UTF8.GetString(data)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ParseMetadata] Unexpected error: {ex.Message}");
        }
        return null;
    }
}