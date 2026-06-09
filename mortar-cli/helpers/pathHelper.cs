using System;
using System.IO;

namespace mortarCli.helpers
{
    public static class pathHelper
    {
        public static string normalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            path = path.Trim();

            try
            {
                if (!Path.IsPathRooted(path))
                    path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), path));
                else
                    path = Path.GetFullPath(path);

                return path;
            }
            catch
            {
                return null;
            }
        }

        public static bool pathsEqual(string a, string b)
        {
            return string.Equals(
                Path.GetFullPath(a),
                Path.GetFullPath(b),
                StringComparison.OrdinalIgnoreCase);
        }
    }
}