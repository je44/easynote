using System.IO;

namespace EasyNote;

internal static class AppPaths
{
    private const string AppFolderName = "easy-note";
    private const string PortableMarkerFileName = "portable.marker";
    private const string PortableDataDirectoryName = "data";

    public static string AppDataDirectory { get; } = ResolveAppDataDirectory();

    private static string ResolveAppDataDirectory()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var portableMarkerPath = Path.Combine(baseDirectory, PortableMarkerFileName);

        if (File.Exists(portableMarkerPath))
            return Path.Combine(baseDirectory, PortableDataDirectoryName);

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppFolderName);
    }
}
