using System;
using System.IO;
using mortarCli.models;
using mortarCli.services;
using mortarCli.helpers;

namespace mortarCli.commands
{
    public static class infoCommand
    {
        public static void execute(string[] args)
        {
            string sourceFile = args.Length > 1 ? args[1] : null;

            var links = storageService.loadLinks();
            if (links == null) return;

            if (links.Count == 0)
            {
                Console.WriteLine("No files linked.");
                return;
            }

            // Interactive picker if no source file provided
            if (string.IsNullOrEmpty(sourceFile))
            {
                sourceFile = showInteractivePicker(links);
                if (sourceFile == null) return;
            }

            string sourceNormalized = pathHelper.normalizePath(sourceFile);
            if (sourceNormalized == null)
            {
                Console.WriteLine($"Error: Invalid source file path \"{sourceFile}\"");
                return;
            }

            var link = links.Find(l => pathHelper.pathsEqual(l.sourceFile, sourceNormalized));
            if (link == null)
            {
                Console.WriteLine($"No links found for {sourceFile}");
                return;
            }

            // Print source file info
            Console.WriteLine();
            Console.WriteLine($"Source File : {link.sourceFile}");
            Console.WriteLine($"Linked At   : {link.linkedAt}");
            Console.WriteLine($"Documents   : {link.documentPaths.Count}");
            Console.WriteLine();

            for (int i = 0; i < link.documentPaths.Count; i++)
            {
                var doc = link.documentPaths[i];
                string status = statusService.getStatus(link.sourceFile, doc);
                string target = !string.IsNullOrEmpty(doc.path)
                    ? doc.path
                    : doc.url ?? "none";

                Console.WriteLine($"  [{i + 1}] {(doc.isPrimary ? "★ PRIMARY" : "")}");
                Console.WriteLine($"      Nickname  : {doc.nickname ?? "-"}");
                Console.WriteLine($"      Type      : {doc.docType ?? "-"}");
                Console.WriteLine($"      Path      : {doc.path ?? "-"}");
                Console.WriteLine($"      URL       : {doc.url ?? "-"}");
                Console.WriteLine($"      Notes     : {doc.notes ?? "-"}");
                Console.WriteLine($"      Sync      : {(doc.outOfDateDetection ? "enabled" : "disabled")}");
                Console.WriteLine($"      Status    : {status}");
                Console.WriteLine();
            }
        }

        private static string showInteractivePicker(System.Collections.Generic.List<docLink> links)
        {
            Console.WriteLine("Linked source files:");
            for (int i = 0; i < links.Count; i++)
            {
                Console.WriteLine($"  {i + 1}. {Path.GetFileName(links[i].sourceFile)}");
            }
            Console.Write("Select file (number): ");

            string input = Console.ReadLine()?.Trim();
            if (!int.TryParse(input, out int choice) ||
                choice < 1 ||
                choice > links.Count)
            {
                Console.WriteLine("Invalid selection.");
                return null;
            }

            return links[choice - 1].sourceFile;
        }
    }
}