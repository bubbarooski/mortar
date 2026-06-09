using System;
using System.IO;
using mortarCli.models;
using mortarCli.services;
using mortarCli.helpers;

namespace mortarCli.commands
{
    public static class unlinkCommand
    {
        public static void execute(string[] args)
        {
            string sourceFile = args.Length > 1 ? args[1] : null;

            // Interactive mode for missing source file
            if (string.IsNullOrEmpty(sourceFile))
            {
                Console.Write("Source file: ");
                sourceFile = Console.ReadLine()?.Trim();
            }

            string sourceNormalized = pathHelper.normalizePath(sourceFile);
            if (sourceNormalized == null)
            {
                Console.WriteLine($"Error: Invalid source file path \"{sourceFile}\"");
                return;
            }

            var links = storageService.loadLinks();
            if (links == null) return;

            var existing = links.Find(l => pathHelper.pathsEqual(l.sourceFile, sourceNormalized));
            if (existing == null)
            {
                Console.WriteLine($"No links found for {sourceFile}");
                return;
            }

            // No third argument — interactive picker
            if (args.Length < 3)
            {
                showInteractivePicker(existing, links);
                return;
            }

            // Check for contradictory flags
            bool hasAll = false;
            bool hasName = false;
            foreach (var a in args)
            {
                if (a.ToLower() == "--all") hasAll = true;
                if (a.ToLower() == "--name") hasName = true;
            }
            if (hasAll && hasName)
            {
                Console.WriteLine("Error: Cannot use --all and --name together.");
                return;
            }

            if (args[2].ToLower() == "--all")
            {
                links.Remove(existing);
                if (!storageService.saveLinks(links)) return;
                Console.WriteLine($"Removed all links from {sourceFile}");
                return;
            }

            documentEntry entry = null;

            if (args[2].ToLower() == "--name")
            {
                if (args.Length < 4)
                {
                    Console.WriteLine("Error: --name requires a value.");
                    return;
                }
                string nickname = args[3].Trim();
                if (string.IsNullOrWhiteSpace(nickname))
                {
                    Console.WriteLine("Error: Nickname cannot be empty or whitespace.");
                    return;
                }
                entry = existing.documentPaths.Find(d =>
                    d.nickname != null &&
                    d.nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase));
                if (entry == null)
                {
                    Console.WriteLine($"No link with nickname \"{nickname}\" found for {sourceFile}");
                    return;
                }
            }
            else if (args[2].StartsWith("--"))
            {
                Console.WriteLine($"Error: Unknown flag \"{args[2]}\"");
                return;
            }
            else
            {
                string documentPath = pathHelper.normalizePath(args[2]);
                if (documentPath == null)
                {
                    Console.WriteLine($"Error: Invalid document path \"{args[2]}\"");
                    return;
                }
                entry = existing.documentPaths.Find(d =>
                    !string.IsNullOrEmpty(d.path) &&
                    pathHelper.pathsEqual(d.path, documentPath));
                if (entry == null)
                {
                    Console.WriteLine($"{args[2]} is not linked to {sourceFile}");
                    return;
                }
            }

            existing.documentPaths.Remove(entry);
            if (existing.documentPaths.Count == 0)
                links.Remove(existing);

            if (!storageService.saveLinks(links)) return;
            Console.WriteLine($"Unlinked \"{entry.path ?? entry.url}\" from {sourceFile}");
        }

        private static void showInteractivePicker(docLink existing, System.Collections.Generic.List<docLink> links)
        {
            Console.WriteLine($"Available links for {Path.GetFileName(existing.sourceFile)}:");
            for (int i = 0; i < existing.documentPaths.Count; i++)
            {
                var doc = existing.documentPaths[i];
                string label = !string.IsNullOrEmpty(doc.nickname)
                    ? doc.nickname
                    : !string.IsNullOrEmpty(doc.path)
                        ? Path.GetFileName(doc.path)
                        : doc.url ?? "unnamed";
                Console.WriteLine($"  {i + 1}. {label}");
            }
            Console.WriteLine($"  {existing.documentPaths.Count + 1}. All");
            Console.Write("Unlink which entry (number): ");

            string input = Console.ReadLine()?.Trim();
            if (!int.TryParse(input, out int choice) ||
                choice < 1 ||
                choice > existing.documentPaths.Count + 1)
            {
                Console.WriteLine("Invalid selection.");
                return;
            }

            if (choice == existing.documentPaths.Count + 1)
            {
                links.Remove(existing);
                if (!storageService.saveLinks(links)) return;
                Console.WriteLine($"Removed all links from {Path.GetFileName(existing.sourceFile)}");
                return;
            }

            var entry = existing.documentPaths[choice - 1];
            existing.documentPaths.Remove(entry);
            if (existing.documentPaths.Count == 0)
                links.Remove(existing);

            if (!storageService.saveLinks(links)) return;
            Console.WriteLine($"Unlinked \"{entry.path ?? entry.url}\" from {Path.GetFileName(existing.sourceFile)}");
        }
    }
}