using System;
using System.Collections.Generic;
using System.IO;
using mortarCli.models;
using mortarCli.services;
using mortarCli.helpers;

namespace mortarCli.commands
{
    public static class linkCommand
    {
        public static void execute(string[] args)
        {
            string sourceFile = args.Length > 1 ? args[1] : null;
            string documentPath = args.Length > 2 && !args[2].StartsWith("--") ? args[2] : null;
            string url = null;
            string nickname = null;
            string docType = null;
            string notes = null;
            bool isPrimary = false;
            bool outOfDateDetection = true;
            int flagStart = documentPath != null ? 3 : 2;

            // Parse flags from args
            for (int i = flagStart; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "--url":
                        if (i + 1 >= args.Length) { Console.WriteLine("Error: --url requires a value."); return; }
                        url = args[++i];
                        break;
                    case "--name":
                        if (i + 1 >= args.Length) { Console.WriteLine("Error: --name requires a value."); return; }
                        nickname = args[++i].Trim();
                        break;
                    case "--type":
                        if (i + 1 >= args.Length) { Console.WriteLine("Error: --type requires a value."); return; }
                        docType = args[++i];
                        break;
                    case "--notes":
                        if (i + 1 >= args.Length) { Console.WriteLine("Error: --notes requires a value."); return; }
                        notes = args[++i];
                        break;
                    case "--primary":
                        isPrimary = true;
                        break;
                    case "--no-sync":
                        outOfDateDetection = false;
                        break;
                    default:
                        if (args[i].StartsWith("--"))
                        {
                            Console.WriteLine($"Error: Unknown flag \"{args[i]}\"");
                            return;
                        }
                        break;
                }
            }

            // Interactive mode for missing required fields
            if (string.IsNullOrEmpty(sourceFile))
            {
                Console.Write("Source file: ");
                sourceFile = Console.ReadLine()?.Trim();
            }

            if (string.IsNullOrEmpty(documentPath) && string.IsNullOrEmpty(url))
            {
                Console.Write("Document path (leave blank to enter URL instead): ");
                documentPath = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(documentPath))
                {
                    Console.Write("URL: ");
                    url = Console.ReadLine()?.Trim();
                }
            }

            if (string.IsNullOrEmpty(nickname))
            {
                Console.Write("Nickname (optional): ");
                nickname = Console.ReadLine()?.Trim();
            }

            if (string.IsNullOrEmpty(docType))
            {
                Console.WriteLine($"Type options: {string.Join(", ", docTypes.all)}");
                Console.Write("Type (optional): ");
                docType = Console.ReadLine()?.Trim();
            }

            if (string.IsNullOrEmpty(notes))
            {
                Console.Write("Notes (optional): ");
                notes = Console.ReadLine()?.Trim();
            }

            if (!isPrimary)
            {
                Console.Write("Primary reference? (y/n, default n): ");
                string primaryInput = Console.ReadLine()?.Trim().ToLower();
                isPrimary = primaryInput == "y";
            }

            if (outOfDateDetection && !string.IsNullOrEmpty(documentPath))
            {
                Console.Write("Enable sync detection? (y/n, default y): ");
                string syncInput = Console.ReadLine()?.Trim().ToLower();
                if (syncInput == "n") outOfDateDetection = false;
            }

            // Validate
            string sourceNormalized = pathHelper.normalizePath(sourceFile);
            if (sourceNormalized == null)
            {
                Console.WriteLine($"Error: Invalid source file path \"{sourceFile}\"");
                return;
            }

            string docNormalized = null;
            if (!string.IsNullOrEmpty(documentPath))
            {
                docNormalized = pathHelper.normalizePath(documentPath);
                if (docNormalized == null)
                {
                    Console.WriteLine($"Error: Invalid document path \"{documentPath}\"");
                    return;
                }
            }

            string validationError = validationHelper.validateEntry(docNormalized, url);
            if (validationError != null)
            {
                Console.WriteLine($"Error: {validationError}");
                return;
            }

            if (!string.IsNullOrEmpty(docNormalized) &&
                pathHelper.pathsEqual(sourceNormalized, docNormalized))
            {
                Console.WriteLine("Error: Source file and document path cannot be the same.");
                return;
            }

            if (!string.IsNullOrEmpty(docType) &&
                !validationHelper.isValidDocType(docType))
            {
                Console.WriteLine($"Error: Invalid doc type \"{docType}\".");
                Console.WriteLine($"Valid types: {string.Join(", ", docTypes.all)}");
                return;
            }

            if (!string.IsNullOrEmpty(nickname) &&
                !validationHelper.isValidNickname(nickname))
            {
                Console.WriteLine("Error: Nickname cannot be empty or whitespace.");
                return;
            }

            if (!File.Exists(sourceNormalized))
                Console.WriteLine($"Warning: Source file does not exist: {sourceNormalized}");

            if (!string.IsNullOrEmpty(docNormalized) && !File.Exists(docNormalized))
                Console.WriteLine($"Warning: Document does not exist: {docNormalized}");

            var links = storageService.loadLinks();
            if (links == null) return;

            var existing = links.Find(l => pathHelper.pathsEqual(l.sourceFile, sourceNormalized));

            if (existing != null)
            {
                if (!string.IsNullOrEmpty(docNormalized) &&
                    existing.documentPaths.Exists(d =>
                        !string.IsNullOrEmpty(d.path) &&
                        pathHelper.pathsEqual(d.path, docNormalized)))
                {
                    Console.WriteLine($"Error: {docNormalized} is already linked to {sourceNormalized}");
                    return;
                }

                if (nickname != null && existing.documentPaths.Exists(d =>
                    d.nickname != null &&
                    d.nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase)))
                {
                    Console.WriteLine($"Error: Nickname \"{nickname}\" is already used for another document on this file.");
                    return;
                }

                existing.documentPaths.Add(new documentEntry
                {
                    path = docNormalized,
                    url = url,
                    nickname = string.IsNullOrEmpty(nickname) ? null : nickname,
                    docType = string.IsNullOrEmpty(docType) ? null : docType,
                    notes = string.IsNullOrEmpty(notes) ? null : notes,
                    isPrimary = isPrimary,
                    outOfDateDetection = outOfDateDetection
                });
            }
            else
            {
                links.Add(new docLink
                {
                    sourceFile = sourceNormalized,
                    documentPaths = new List<documentEntry>
                    {
                        new documentEntry
                        {
                            path = docNormalized,
                            url = url,
                            nickname = string.IsNullOrEmpty(nickname) ? null : nickname,
                            docType = string.IsNullOrEmpty(docType) ? null : docType,
                            notes = string.IsNullOrEmpty(notes) ? null : notes,
                            isPrimary = isPrimary,
                            outOfDateDetection = outOfDateDetection
                        }
                    },
                    linkedAt = DateTime.UtcNow.ToString("o")
                });
            }

            if (!storageService.saveLinks(links)) return;

            string label = !string.IsNullOrEmpty(nickname) ? $" (\"{nickname}\")" : "";
            string target = !string.IsNullOrEmpty(docNormalized) ? docNormalized : url;
            Console.WriteLine($"Linked {sourceNormalized} -> {target}{label}");
        }
    }
}