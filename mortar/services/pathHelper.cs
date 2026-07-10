using System;
using System.IO;

namespace mortar.services
{
    public static class pathHelper
    {
        public static bool pathsEqual(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
                return false;
            try
            {
                return string.Equals(
                    Path.GetFullPath(a),
                    Path.GetFullPath(b),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        public static string? normalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            path = path.Trim();

            try
            {
                if (!Path.IsPathRooted(path))
                    path = Path.GetFullPath(Path.Combine(
                        Directory.GetCurrentDirectory(), path));
                else
                    path = Path.GetFullPath(path);

                return path;
            }
            catch { return null; }
        }

        public static string makeRelativePath(string basePath, string fullPath)
        {
            if (string.IsNullOrEmpty(basePath) || string.IsNullOrEmpty(fullPath))
                return fullPath;

            try
            {
                if (!basePath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    basePath += Path.DirectorySeparatorChar;

                var baseUri = new Uri(basePath);
                var fullUri = new Uri(fullPath);

                if (!baseUri.Scheme.Equals(fullUri.Scheme, StringComparison.OrdinalIgnoreCase))
                    return fullPath;

                var relativeUri = baseUri.MakeRelativeUri(fullUri);
                return Uri.UnescapeDataString(relativeUri.ToString())
                          .Replace('/', Path.DirectorySeparatorChar);
            }
            catch { return fullPath; }
        }

        public static string? resolveRelativePath(string basePath, string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (Path.IsPathRooted(path)) return path;
            try
            {
                return Path.GetFullPath(Path.Combine(basePath, path));
            }
            catch { return path; }
        }
    }
}