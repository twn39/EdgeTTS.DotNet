namespace EdgeTTS.NET.Text;

using System.Text;
using System.Net;


/// <summary>
/// Splits text into chunks by byte length, respecting UTF-8 and XML entities.
/// </summary>
internal static class TextSplitter
{
    public static IEnumerable<string> SplitTextByByteLength(string text, int byteLength)
    {
        if (byteLength <= 0)
            throw new ArgumentException("byteLength must be greater than 0", nameof(byteLength));

        var encodedText = Encoding.UTF8.GetBytes(text);
        var remainingBytes = new ReadOnlyMemory<byte>(encodedText);

        while (remainingBytes.Length > byteLength)
        {
            var splitAt = FindLastNewlineOrSpace(remainingBytes.Span, byteLength);

            if (splitAt < 0)
            {
                splitAt = FindSafeUtf8SplitPoint(remainingBytes.Span[..byteLength]);
            }

            if (splitAt > 0)
            {
                splitAt = AdjustForXmlEntity(remainingBytes.Span, splitAt);
            }

            if (splitAt <= 0)
            {
                // If we can't find a safe split point (e.g. at the start of an entity that's too long)
                // we have no choice but to split at a safe UTF-8 boundary within the limit.
                splitAt = FindSafeUtf8SplitPoint(remainingBytes.Span[..byteLength]);
                if (splitAt <= 0) splitAt = 1; // Force at least one byte to prevent infinite loop
            }

            var chunkBytes = remainingBytes[..splitAt].ToArray();
            var chunkString = Encoding.UTF8.GetString(chunkBytes).Trim();
            if (!string.IsNullOrEmpty(chunkString))
            {
                yield return chunkString;
            }

            remainingBytes = remainingBytes[splitAt..];
            
            // Consume leading whitespace for next chunk to avoid splitAt=0 issues
            while (remainingBytes.Length > 0 && (remainingBytes.Span[0] == ' ' || remainingBytes.Span[0] == '\n' || remainingBytes.Span[0] == '\r'))
            {
                remainingBytes = remainingBytes[1..];
            }
        }

        var lastChunk = Encoding.UTF8.GetString(remainingBytes.Span).Trim();
        if (!string.IsNullOrEmpty(lastChunk))
        {
            yield return lastChunk;
        }
    }

    private static int FindLastNewlineOrSpace(ReadOnlySpan<byte> text, int limit)
    {
        var searchSlice = text[..Math.Min(limit, text.Length)];
        var splitAt = searchSlice.LastIndexOf((byte)'\n');
        if (splitAt < 0)
        {
            splitAt = searchSlice.LastIndexOf((byte)' ');
        }
        return splitAt;
    }

    private static int FindSafeUtf8SplitPoint(ReadOnlySpan<byte> textSegment)
    {
        var splitAt = textSegment.Length;
        while (splitAt > 0)
        {
            try
            {
                Encoding.UTF8.GetString(textSegment[..splitAt]);
                return splitAt;
            }
            catch (ArgumentException) // DecoderFallbackException is internal
            {
                splitAt--;
            }
        }
        return 0;
    }

    private static int AdjustForXmlEntity(ReadOnlySpan<byte> text, int splitAt)
    {
        var slice = text[..splitAt];
        var ampersandIndex = slice.LastIndexOf((byte)'&');
        if (ampersandIndex != -1)
        {
            // Check if a semicolon exists between the ampersand and the split point
            var semicolonIndex = slice[ampersandIndex..].IndexOf((byte)';');
            if (semicolonIndex == -1)
            {
                // Unterminated entity, move split point to before the ampersand
                return ampersandIndex;
            }
        }
        return splitAt;
    }
}