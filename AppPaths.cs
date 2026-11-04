using System.IO;

namespace NekoDex;

/// <summary>
/// Datenordner unter <c>%LocalAppData%\NekoDex</c>.
/// Ein vorhandener Ordner <c>MangaLibraryApp</c> wird einmalig dorthin verschoben.
/// </summary>
public static class AppPaths
{
    public static string DataDirectory { get; } = Resolve();

    private static string Resolve()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var current = Path.Combine(root, "NekoDex");
        var previous = Path.Combine(root, "MangaLibraryApp");
        if (Directory.Exists(previous) && !Directory.Exists(current))
        {
            try
            {
                Directory.Move(previous, current);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return previous;
            }
        }

        return Directory.Exists(current) || !Directory.Exists(previous) ? current : previous;
    }
}
