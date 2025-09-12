namespace EdgeTTS.NET.Text;

using System.Text;
using System.Web;


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

            splitAt = AdjustForXmlEntity(remainingBytes.Span, splitAt);

            if (splitAt <= 0)
            {
                // Cannot find a safe split point, force split at byteLength
                // This might break things but prevents an infinite loop.
                splitAt = byteLength;
            }

            var chunkBytes = remainingBytes[..splitAt].ToArray();
            var chunkString = Encoding.UTF8.GetString(chunkBytes).Trim();
            if (!string.IsNullOrEmpty(chunkString))
            {
                yield return chunkString;
            }

            remainingBytes = remainingBytes[splitAt..];
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