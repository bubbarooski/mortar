using System;
using System.IO;
using mortarCli.models;

namespace mortarCli.services
{
    public static class statusService
    {
        public static string getStatus(string sourceFile, documentEntry doc)
        {
            if (!doc.outOfDateDetection)
                return "SYNC OFF";

            if (string.IsNullOrEmpty(doc.path))
                return "URL ONLY";

            if (!File.Exists(sourceFile))
                return "SRC NOT FOUND";

            if (!File.Exists(doc.path))
                return "DOC NOT FOUND";

            try
            {
                DateTime srcModified = File.GetLastWriteTimeUtc(sourceFile);
                DateTime docModified = File.GetLastWriteTimeUtc(doc.path);
                return docModified > srcModified ? "OUT OF DATE" : "UP TO DATE";
            }
            catch
            {
                return "READ ERROR";
            }
        }
    }
}