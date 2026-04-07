using Stride.Core.IO;

namespace Notima.Stride;

internal sealed class GlobalVirtualFileProvider : IVirtualFileProvider
{
    public string? RootPath => null;

    public string GetAbsolutePath(string path) => VirtualFileSystem.GetAbsolutePath(NormalizePath(path));

    public bool TryGetFileLocation(string path, out string filePath, out long start, out long end)
    {
        try
        {
            filePath = GetAbsolutePath(path);
            start = 0;
            end = -1;
            return true;
        }
        catch
        {
            filePath = string.Empty;
            start = 0;
            end = -1;
            return false;
        }
    }

    public Stream OpenStream(string path, VirtualFileMode mode, VirtualFileAccess access, VirtualFileShare share = VirtualFileShare.Read, StreamFlags streamFlags = StreamFlags.None)
        => VirtualFileSystem.OpenStream(NormalizePath(path), mode, access, share);

    public string[] ListFiles(string path, string searchPattern, VirtualSearchOption searchOption)
        => VirtualFileSystem.ListFiles(NormalizePath(path), searchPattern, searchOption).GetAwaiter().GetResult();

    public void CreateDirectory(string url) => VirtualFileSystem.CreateDirectory(NormalizePath(url));

    public bool DirectoryExists(string url) => VirtualFileSystem.DirectoryExists(NormalizePath(url));

    public bool FileExists(string url) => VirtualFileSystem.FileExists(NormalizePath(url));

    public void FileDelete(string url) => VirtualFileSystem.FileDelete(NormalizePath(url));

    public void FileMove(string sourceUrl, string destinationUrl) => VirtualFileSystem.FileMove(NormalizePath(sourceUrl), NormalizePath(destinationUrl));

    public void FileMove(string sourceUrl, IVirtualFileProvider destinationProvider, string destinationUrl)
    {
        if (destinationProvider is GlobalVirtualFileProvider)
        {
            VirtualFileSystem.FileMove(sourceUrl, destinationUrl);
            return;
        }

        throw new NotSupportedException("Cross-provider file moves are not supported by GlobalVirtualFileProvider.");
    }

    public long FileSize(string url) => VirtualFileSystem.FileSize(NormalizePath(url));

    public DateTime GetLastWriteTime(string url) => VirtualFileSystem.GetLastWriteTime(NormalizePath(url));

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        var normalized = path.Replace('\\', '/');
        if (normalized.StartsWith("shaders/", StringComparison.Ordinal))
        {
            return "/" + normalized;
        }

        return normalized;
    }

    public void Dispose()
    {
    }
}

