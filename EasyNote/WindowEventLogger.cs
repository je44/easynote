using System.IO;
using System.Text;

namespace EasyNote;

internal static class WindowEventLogger
{
    private static readonly object SyncRoot = new();
    private static readonly string LogDirectory = AppPaths.AppDataDirectory;
    private static readonly string LogPath = Path.Combine(LogDirectory, "window-events.log");
    private static readonly string ArchivePath = Path.Combine(LogDirectory, "window-events.previous.log");
    private const long MaxLogBytes = 2 * 1024 * 1024;

    public static string CurrentLogPath => LogPath;

    public static void Write(string source, string message)
    {
        try
        {
            lock (SyncRoot)
            {
                Directory.CreateDirectory(LogDirectory);
                RotateIfNeeded();

                var line = new StringBuilder(256)
                    .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))
                    .Append(" | pid=")
                    .Append(Environment.ProcessId)
                    .Append(" | tid=")
                    .Append(Environment.CurrentManagedThreadId)
                    .Append(" | ")
                    .Append(source)
                    .Append(" | ")
                    .Append(message)
                    .AppendLine()
                    .ToString();

                File.AppendAllText(LogPath, line, Encoding.UTF8);
            }
        }
        catch
        {
        }
    }

    private static void RotateIfNeeded()
    {
        if (!File.Exists(LogPath))
            return;

        var length = new FileInfo(LogPath).Length;
        if (length < MaxLogBytes)
            return;

        if (File.Exists(ArchivePath))
            File.Delete(ArchivePath);

        File.Move(LogPath, ArchivePath);
    }
}
