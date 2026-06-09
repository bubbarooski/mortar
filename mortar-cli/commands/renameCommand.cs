using System;
using System.IO;
using mortarCli.models;
using mortarCli.services;
using mortarCli.helpers;

namespace mortarCli.commands
{
    public static class renameCommand
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

            documentEntry entry = null;
            string newNickname = null;

            // No third argument — interactive picker
            if (args.Length < 3)
            {
                entry = showInteractivePicker(existing);
                if (entry == null) return;

                Console.Write("New nickname: ");
                newNickname = Console.ReadLine()?.Trim();
            }
            else if (args[2].ToLower() == "--name")
            {
                if (args.Length < 5)
                {
                    Console.WriteLine("Usage: mortar-cli rename <sourceFile> --name <oldNickname> <newNickname>");
                    return;
                }
                string oldNickname = args[3].Trim();
                newNickname = args[4].Trim();

                if (string.IsNullOrWhiteSpace(oldNickname))
                {
                    Console.WriteLine("Error: Old nickname cannot be empty or whitespace.");
                    return;
                }

                entry = existing.documentPaths.Find(d =>
                    d.nickname != null &&
                    d.nickname.Equals(oldNickname, StringComparison.OrdinalIgnoreCase));
                if (entry == null)
                {
                    Console.WriteLine($"No link with nickname \"{oldNickname}\" found for {sourceFile}");
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
                if (args.Length < 4)
                {
                    Console.WriteLine("Usage: mortar-cli rename <sourceFile> <documentPath> <newNickname>");
                    return;
                }

                string documentPath = pathHelper.normalizePath(args[2]);
                if (documentPath == null)
                {
                    Console.WriteLine($"Error: Invalid document path \"{args[2]}\"");
                    return;
                }
                newNickname = args[3].Trim();
                entry = existing.documentPaths.Find(d =>
                    !string.IsNullOrEmpty(d.path) &&
                    pathHelper.pathsEqual(d.path, documentPath));
                if (entry == null)
                {
                    Console.WriteLine($"{args[2]} is not linked to {sourceFile}");
                    return;
                }
            }

            if (string.IsNullOrWhiteSpace(newNickname))
            {
                Console.WriteLine("Error: New nickname cannot be empty or whitespace.");
                return;
            }

            if (entry.nickname != null &&
                entry.nickname.Equals(newNickname, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Warning: Already has nickname \"{newNickname}\"");
                return;
            }

            if (existing.documentPaths.Exists(d =>
                d != entry &&
                d.nickname != null &&
                d.nickname.Equals(newNickname, StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine($"Error: Nickname \"{newNickname}\" is already used for another document on this file.");
                return;
            }

            string oldName = entry.nickname ?? "none";
            entry.nickname = newNickname;
            if (!storageService.saveLinks(links)) return;
            Console.WriteLine($"Renamed \"{oldName}\" to \"{newNickname}\" for {entry.path ?? entry.url}");
        }

        private static documentEntry showInteractivePicker(docLink existing)
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
            Console.Write("Rename which entry (number): ");

            string input = Console.ReadLine()?.Trim();
            if (!int.TryParse(input, out int choice) ||
                choice < 1 ||
                choice > existing.documentPaths.Count)
            {
                Console.WriteLine("Invalid selection.");
                return null;
            }

            return existing.documentPaths[choice - 1];
        }
    }
}