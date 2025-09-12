// See https://aka.ms/new-console-template for more information

using EdgeTTS.NET;
using EdgeTTS.NET.Models;


using System.Diagnostics; // 添加此 using 语句
using System.IO;          // 添加此 using 语句

Console.WriteLine("EdgeTTS C# Demo");
Console.WriteLine("===============");

// --- 1. 列出所有可用的中文音色 ---
Console.WriteLine("\n[1] Listing available Chinese voices...");
try
{
    var allVoices = await Voices.ListVoicesAsync();
    var chineseVoices = allVoices
        .Where(v => v.Locale.StartsWith("zh-"))
        .OrderBy(v => v.ShortName)
        .ToList();

    Console.WriteLine($"Found {chineseVoices.Count} Chinese voices:");
    foreach (var voice in chineseVoices)
    {
        Console.WriteLine($" - {voice.ShortName,-20} | Gender: {voice.Gender,-6} | Locale: {voice.Locale}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error listing voices: {ex.Message}");
}

// --- 2. 将一段简单的文本合成为 MP3 文件 ---
Console.WriteLine("\n[2] Synthesizing a simple text to 'hello_world.mp3'...");
try
{
    var text = "Hello World! This is a test from C# EdgeTTS library.";
    var communicate = new Communicate(text, "en-US-EmmaMultilingualNeural");

    var audioFilePath = "hello_world.mp3";
    await communicate.SaveAsync(audioFilePath);

    Console.WriteLine($"Successfully saved audio to {Path.GetFullPath(audioFilePath)}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error synthesizing audio: {ex.Message}");
}


// --- 3. 流式处理一段中文文本，并打印出音频和元数据信息，并在 macOS 上播放 ---
Console.WriteLine("\n[3] Streaming a Chinese text and printing chunk info, then playing on macOS...");
try
{
    var baseText = "微软的语音合成技术非常先进，它能够生成自然流畅的语音。这个 C# 库让你可以在 MAUI 和其他 .NET 应用中轻松使用这项技术。";
    var longText = "";
    for (int i = 0; i < 3; i++) // 重复 3 次，使其成为一个较长的文本，避免文件过大
    {
        longText += baseText + $" (这是第 {i + 1} 段。)";
    }
    longText += "现在，我们来听听这段长文本的合成效果如何。希望一切顺利！";


    // 使用晓晓的音色
    var communicate = new Communicate(longText, "zh-CN-XiaoxiaoNeural");

    Console.WriteLine("Starting stream...");
    var audioBytesTotal = 0;
    var wordCount = 0;

    // 创建一个临时文件来保存音频数据
    var tempAudioFilePath = Path.Combine(Path.GetTempPath(), $"edgetts_demo_audio_{Guid.NewGuid():N}.mp3");
    Console.WriteLine($"Saving streamed audio to temporary file: {tempAudioFilePath}");

    await using (var tempFileStream = new FileStream(tempAudioFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
    {
        await foreach (var chunk in communicate.StreamAsync())
        {
            switch (chunk)
            {
                case AudioChunk audioChunk:
                    await tempFileStream.WriteAsync(audioChunk.Data); // 将音频数据写入临时文件
                    audioBytesTotal += audioChunk.Data.Length;
                    // Console.WriteLine($"Received audio chunk, size: {audioChunk.Data.Length} bytes.");
                    break;

                case MetadataChunk metadataChunk:
                    wordCount++;
                    // Console.WriteLine($"Received metadata for word: '{metadataChunk.Text}', " +
                    //                   $"Offset: {metadataChunk.Offset.TotalSeconds:F3}s, " +
                    //                   $"Duration: {metadataChunk.Duration.TotalSeconds:F3}s");
                    break;

                case TurnEndChunk:
                    Console.WriteLine("--- Turn End ---");
                    break;
            }
        }
    } // tempFileStream 会在此处自动关闭和释放

    Console.WriteLine("\nStream finished.");
    Console.WriteLine($"Total audio bytes received: {audioBytesTotal}");
    Console.WriteLine($"Total words with metadata: {wordCount}");

    // --- 本地播放逻辑 ---
    Console.WriteLine("\nAttempting to play audio locally...");
    if (File.Exists(tempAudioFilePath))
    {
        // 检查操作系统是否是 macOS
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
        {
            try
            {
                // 使用 afplay 命令播放 MP3 文件
                Process.Start("afplay", $"\"{tempAudioFilePath}\"");
                Console.WriteLine($"Playing audio using 'afplay': {tempAudioFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error playing audio with 'afplay': {ex.Message}");
                Console.WriteLine("Please ensure 'afplay' is available in your PATH.");
            }
        }
        else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
        {
            try
            {
                // Windows 可以尝试使用 start 命令或 MediaPlayer
                // 这里为了简单，使用 start 命令，它会用默认播放器打开文件
                Process.Start(new ProcessStartInfo(tempAudioFilePath) { UseShellExecute = true });
                Console.WriteLine($"Playing audio using default player on Windows: {tempAudioFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error playing audio on Windows: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("Local audio playback is not implemented for this OS.");
        }
    }
    else
    {
        Console.WriteLine("Temporary audio file not found.");
    }

    // 可选：播放完毕后删除临时文件
    // Console.WriteLine("Press any key to delete temporary audio file and exit.");
    // Console.ReadKey();
    // File.Delete(tempAudioFilePath);
}
catch (Exception ex)
{
    Console.WriteLine($"Error during streaming or playback: {ex.Message}");
}

Console.WriteLine("\nDemo finished. Press any key to exit.");
Console.ReadKey();