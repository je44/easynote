using System.IO;

namespace EasyNote;

internal static class AppPaths
{
    private const string AppFolderName = "easy-note";
    private const string PortableMarkerFileName = "portable.marker";
    private const string PortableDataDirectoryName = "data";
    private const string TodoStateFileName = "todos.json";
    private const string WindowStateFileName = "window-state.json";

    public static string AppDataDirectory { get; } = ResolveAppDataDirectory();

    private static string ResolveAppDataDirectory()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var portableMarkerPath = Path.Combine(baseDirectory, PortableMarkerFileName);

        if (File.Exists(portableMarkerPath))
        {
            var portableDataDirectory = Path.Combine(baseDirectory, PortableDataDirectoryName);
            var legacyDataDirectory = ResolveLegacyAppDataDirectory();
            EnsurePortableDataMigrated(portableDataDirectory, legacyDataDirectory);
            return portableDataDirectory;
        }

        return ResolveLegacyAppDataDirectory();
    }

    private static string ResolveLegacyAppDataDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppFolderName);
    }

    private static void EnsurePortableDataMigrated(string portableDataDirectory, string legacyDataDirectory)
    {
        if (string.Equals(portableDataDirectory, legacyDataDirectory, StringComparison.OrdinalIgnoreCase))
            return;

        CopyLegacyFileIfMissing(portableDataDirectory, legacyDataDirectory, TodoStateFileName);
        CopyLegacyFileIfMissing(portableDataDirectory, legacyDataDirectory, WindowStateFileName);
    }

    private static void CopyLegacyFileIfMissing(string portableDataDirectory, string legacyDataDirectory, string fileName)
    {
        var sourcePath = Path.Combine(legacyDataDirectory, fileName);
        var targetPath = Path.Combine(portableDataDirectory, fileName);

        if (!File.Exists(sourcePath) || File.Exists(targetPath))
            return;

        try
        {
            Directory.CreateDirectory(portableDataDirectory);
            using var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var target = new FileStream(targetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            source.CopyTo(target);
        }
        catch
        {
            // Migration is best-effort; normal loading/saving still handles missing files.
        }
    }
}
