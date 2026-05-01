using System.IO;
using System.Text.Json;

namespace EasyNote;

internal static class LocalUserDataStore
{
    private const int CurrentSchemaVersion = 1;
    private const string AppFolderName = "easy-note";
    private const string AppExeFileName = "EasyNote.exe";
    private const string PortableDataDirectoryName = "data";
    private const string ManifestFileName = "user-data.json";
    private const string TodoStateFileName = "todos.json";
    private const string WindowStateFileName = "window-state.json";
    private const int MaxDriveDirectoriesToInspect = 1200;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public static string DataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        AppFolderName);

    public static string ManifestPath => Path.Combine(DataDirectory, ManifestFileName);
    public static string TodoStatePath => Path.Combine(DataDirectory, TodoStateFileName);
    public static string WindowStatePath => Path.Combine(DataDirectory, WindowStateFileName);

    public static void EnsureInitialized()
    {
        Directory.CreateDirectory(DataDirectory);
        MigratePortableDataIfNeeded();
        EnsureTextFile(TodoStatePath, "[]");
        WriteManifest();
    }

    public static T? ReadJson<T>(string path)
    {
        EnsureInitialized();

        if (!File.Exists(path))
            return default;

        return JsonSerializer.Deserialize<T>(File.ReadAllText(path));
    }

    public static void WriteJson<T>(string path, T value)
    {
        EnsureInitialized();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var json = JsonSerializer.Serialize(value, SerializerOptions);
        WriteAllTextAtomic(path, json);
    }

    private static void MigratePortableDataIfNeeded()
    {
        if (!ShouldImportLegacyPortableData())
            return;

        var sourceDirectory = FindBestPortableDataDirectory();
        if (sourceDirectory == null)
            return;

        CopyFileIfTargetMissingOrEmpty(sourceDirectory, TodoStateFileName);
        CopyFileIfTargetMissingOrEmpty(sourceDirectory, WindowStateFileName);
    }

    private static bool ShouldImportLegacyPortableData()
    {
        if (!File.Exists(WindowStatePath))
            return true;

        return IsMissingOrEmptyTodoFile(TodoStatePath);
    }

    private static string? FindBestPortableDataDirectory()
    {
        var currentDataDirectory = Path.Combine(AppContext.BaseDirectory, PortableDataDirectoryName);

        return EnumerateLikelyPortableDataDirectories()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(path => !string.Equals(path, DataDirectory, StringComparison.OrdinalIgnoreCase))
            .Where(HasImportableData)
            .OrderByDescending(GetLastDataWriteTimeUtc)
            .ThenByDescending(path => string.Equals(path, currentDataDirectory, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();
    }

    private static IEnumerable<string> EnumerateLikelyPortableDataDirectories()
    {
        foreach (var root in EnumerateLikelyPortableRoots())
        {
            var dataDirectory = Path.Combine(root, PortableDataDirectoryName);
            if (Directory.Exists(dataDirectory))
                yield return dataDirectory;
        }
    }

    private static IEnumerable<string> EnumerateLikelyPortableRoots()
    {
        var baseDirectory = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        yield return baseDirectory;

        foreach (var nearby in EnumerateNearbyDirectories(baseDirectory))
            yield return nearby;

        foreach (var knownLocation in EnumerateKnownUserLocations())
        {
            yield return knownLocation;

            foreach (var child in SafeEnumerateDirectories(knownLocation))
                yield return child;

            foreach (var grandchild in SafeEnumerateDirectories(knownLocation)
                         .Where(IsLikelyEasyNoteDirectory)
                         .SelectMany(SafeEnumerateDirectories))
            {
                yield return grandchild;
            }
        }

        foreach (var driveDirectory in EnumerateDriveRootDirectories())
            yield return driveDirectory;
    }

    private static IEnumerable<string> EnumerateNearbyDirectories(string baseDirectory)
    {
        var current = new DirectoryInfo(baseDirectory);
        for (var depth = 0; current.Parent != null && depth < 2; depth++, current = current.Parent)
        {
            yield return current.Parent.FullName;

            foreach (var sibling in SafeEnumerateDirectories(current.Parent.FullName)
                         .Where(IsLikelyEasyNoteDirectory))
            {
                yield return sibling;
            }
        }
    }

    private static IEnumerable<string> EnumerateKnownUserLocations()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var downloads = string.IsNullOrWhiteSpace(userProfile) ? string.Empty : Path.Combine(userProfile, "Downloads");

        return new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                downloads,
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                userProfile
            }
            .Where(path => !string.IsNullOrWhiteSpace(path) && Directory.Exists(path));
    }

    private static IEnumerable<string> EnumerateDriveRootDirectories()
    {
        foreach (var drive in DriveInfo.GetDrives().Where(drive => drive.IsReady))
        {
            var inspected = 0;
            foreach (var child in SafeEnumerateDirectories(drive.RootDirectory.FullName))
            {
                if (inspected++ >= MaxDriveDirectoriesToInspect)
                    break;

                yield return child;

                if (!IsLikelyEasyNoteDirectory(child))
                    continue;

                foreach (var grandchild in SafeEnumerateDirectories(child))
                    if (inspected++ < MaxDriveDirectoriesToInspect)
                        yield return grandchild;
            }
        }
    }

    private static IEnumerable<string> SafeEnumerateDirectories(string path)
    {
        try
        {
            return Directory.EnumerateDirectories(path).ToArray();
        }
        catch
        {
            return [];
        }
    }

    private static bool IsLikelyEasyNoteDirectory(string path)
    {
        var name = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return name.Contains("easynote", StringComparison.OrdinalIgnoreCase)
            || name.Contains("easy-note", StringComparison.OrdinalIgnoreCase)
            || name.Contains("easy note", StringComparison.OrdinalIgnoreCase)
            || name.Contains("note", StringComparison.OrdinalIgnoreCase)
            || name.Contains("便签", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasImportableData(string dataDirectory)
    {
        var parentDirectory = Directory.GetParent(dataDirectory)?.FullName;
        if (parentDirectory == null)
            return false;

        if (!File.Exists(Path.Combine(parentDirectory, AppExeFileName)) && !IsLikelyEasyNoteDirectory(parentDirectory))
            return false;

        return IsImportableJson(Path.Combine(dataDirectory, TodoStateFileName))
            || IsImportableJson(Path.Combine(dataDirectory, WindowStateFileName));
    }

    private static bool IsImportableJson(string path)
    {
        if (!File.Exists(path))
            return false;

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            return document.RootElement.ValueKind is JsonValueKind.Array or JsonValueKind.Object;
        }
        catch
        {
            return false;
        }
    }

    private static DateTime GetLastDataWriteTimeUtc(string dataDirectory)
    {
        var todoPath = Path.Combine(dataDirectory, TodoStateFileName);
        var statePath = Path.Combine(dataDirectory, WindowStateFileName);
        var todoTime = File.Exists(todoPath) ? File.GetLastWriteTimeUtc(todoPath) : DateTime.MinValue;
        var stateTime = File.Exists(statePath) ? File.GetLastWriteTimeUtc(statePath) : DateTime.MinValue;
        return todoTime > stateTime ? todoTime : stateTime;
    }

    private static void CopyFileIfTargetMissingOrEmpty(string sourceDirectory, string fileName)
    {
        var sourcePath = Path.Combine(sourceDirectory, fileName);
        var targetPath = Path.Combine(DataDirectory, fileName);

        if (!File.Exists(sourcePath) || !ShouldImportFile(targetPath, fileName))
            return;

        try
        {
            Directory.CreateDirectory(DataDirectory);
            using var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var target = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
            source.CopyTo(target);
        }
        catch
        {
            // Migration is best-effort; normal first-run initialization still creates usable files.
        }
    }

    private static bool ShouldImportFile(string targetPath, string fileName)
    {
        if (!File.Exists(targetPath))
            return true;

        return fileName == TodoStateFileName && IsMissingOrEmptyTodoFile(targetPath);
    }

    private static bool IsMissingOrEmptyTodoFile(string path)
    {
        if (!File.Exists(path))
            return true;

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            return document.RootElement.ValueKind == JsonValueKind.Array
                && document.RootElement.GetArrayLength() == 0;
        }
        catch
        {
            return false;
        }
    }

    private static void EnsureTextFile(string path, string contents)
    {
        if (File.Exists(path))
            return;

        WriteAllTextAtomic(path, contents);
    }

    private static void WriteManifest()
    {
        var manifest = new UserDataManifest
        {
            SchemaVersion = CurrentSchemaVersion,
            DataDirectory = DataDirectory,
            UpdatedAt = DateTimeOffset.Now
        };

        WriteAllTextAtomic(ManifestPath, JsonSerializer.Serialize(manifest, SerializerOptions));
    }

    private static void WriteAllTextAtomic(string path, string contents)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var tempPath = $"{path}.{Environment.ProcessId}.{Guid.NewGuid():N}.tmp";
        File.WriteAllText(tempPath, contents);

        if (File.Exists(path))
        {
            File.Replace(tempPath, path, null);
        }
        else
        {
            File.Move(tempPath, path);
        }
    }

    private sealed class UserDataManifest
    {
        public int SchemaVersion { get; init; }
        public string DataDirectory { get; init; } = string.Empty;
        public DateTimeOffset UpdatedAt { get; init; }
    }
}
