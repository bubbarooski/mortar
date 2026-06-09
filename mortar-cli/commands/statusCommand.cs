using System;
using System.IO;
using System.Linq;
using mortarCli.models;
using mortarCli.services;
using mortarCli.helpers;

namespace mortarCli.commands
{
    public static class statusCommand
    {
        public static void execute(string[] args)
        {
            string typeFilter = null;

            for (int i = 1; i < args.Length; i++)
            {
                if (args[i].ToLower() == "--type")
                {
                    if (i + 1 >= args.Length)
                    {
                        Console.WriteLine("Error: --type requires a value.");
                        return;
                    }
                    typeFilter = args[++i].ToLower();
                    if (!validationHelper.isValidDocType(typeFilter))
                    {
                        Console.WriteLine($"Error: Invalid doc type \"{typeFilter}\".");
                        Console.WriteLine($"Valid types: {string.Join(", ", docTypes.all)}");
                        return;
                    }
                }
                else if (args[i].StartsWith("--"))
                {
                    Console.WriteLine($"Error: Unknown flag \"{args[i]}\"");
                    return;
                }
            }

            var links = storageService.loadLinks();
            if (links == null) return;

            if (links.Count == 0)
            {
                Console.WriteLine("No files linked.");
                return;
            }

            // Apply type filter if provided
            var filtered = links.Select(l => new
            {
                l.sourceFile,
                docs = l.documentPaths.Where(d =>
                    string.IsNullOrEmpty(typeFilter) ||
                    (d.docType != null &&
                    d.docType.Equals(typeFilter, StringComparison.OrdinalIgnoreCase))
                ).ToList()
            }).Where(l => l.docs.Count > 0).ToList();

            if (filtered.Count == 0)
            {
                Console.WriteLine($"No links found for type \"{typeFilter}\".");
                return;
            }

            // Truncate helper
            string truncate(string s, int max) =>
                s == null ? "-" :
                s.Length <= max ? s : "..." + s.Substring(s.Length - (max - 3));

            // Print header
            Console.WriteLine(
                $"{"Source File",-25} " +
                $"{"Nickname",-20} " +
                $"{"Type",-12} " +
                $"{"Status",-15} " +
                $"{"Document/URL",-35} " +
                $"{"Primary"}");
            Console.WriteLine(new string('-', 115));

            foreach (var link in filtered)
            {
                foreach (var doc in link.docs)
                {
                    string star = doc.isPrimary ? "★" : "";
                    string srcDisplay = truncate(Path.GetFileName(link.sourceFile), 25);
                    string nickname = truncate(doc.nickname, 20);
                    string docType = truncate(doc.docType, 12);
                    string status = statusService.getStatus(link.sourceFile, doc);
                    string target = !string.IsNullOrEmpty(doc.path)
                        ? truncate(doc.path, 35)
                        : truncate(doc.url, 35);

                    Console.WriteLine(
                        $"{srcDisplay,-25} " +
                        $"{nickname,-20} " +
                        $"{docType,-12} " +
                        $"{status,-15} " +
                        $"{target,-35} " +
                        $"{star}");
                }
            }
        }
    }
}