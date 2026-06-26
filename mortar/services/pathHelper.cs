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
            return string.Equals(
                Path.GetFullPath(a),
                Path.GetFullPath(b),
                StringComparison.OrdinalIgnoreCase);
        }

        public static string normalizePath(string path)
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
            catch
            {
                return null;
            }
        }
    }
}

    