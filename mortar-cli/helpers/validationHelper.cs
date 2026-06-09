using System;
using mortarCli.models;

namespace mortarCli.helpers
{
    public static class validationHelper
    {
        public static bool isValidUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            return Uri.TryCreate(url, UriKind.Absolute, out Uri result)
                && (result.Scheme == Uri.UriSchemeHttp
                || result.Scheme == Uri.UriSchemeHttps);
        }

        public static bool isValidDocType(string docType)
        {
            if (string.IsNullOrWhiteSpace(docType))
                return false;

            return Array.Exists(docTypes.all, t =>
                t.Equals(docType, StringComparison.OrdinalIgnoreCase));
        }

        public static bool isValidNickname(string nickname)
        {
            return !string.IsNullOrWhiteSpace(nickname);
        }

        public static bool isValidNotes(string notes)
        {
            return !string.IsNullOrWhiteSpace(notes);
        }

        public static string validateEntry(string path, string url)
        {
            if (string.IsNullOrWhiteSpace(path) && string.IsNullOrWhiteSpace(url))
                return "At least one of path or url is required.";

            if (!string.IsNullOrWhiteSpace(url) && !isValidUrl(url))
                return $"Invalid URL: {url}. Must start with http:// or https://";

            return null;
        }
    }
}